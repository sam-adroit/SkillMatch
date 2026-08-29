[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5227",
    [PSCredential]$StudentCredential,
    [PSCredential]$AdminCredential,
    [switch]$GenerateRecommendation
)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd("/")
$checks = [System.Collections.Generic.List[object]]::new()

function Add-Pass([string]$Name, [string]$Evidence) {
    $checks.Add([pscustomobject]@{ Check = $Name; Result = "PASS"; Evidence = $Evidence })
}

function Invoke-Login([PSCredential]$Credential) {
    $body = @{
        email = $Credential.UserName
        password = $Credential.GetNetworkCredential().Password
    } | ConvertTo-Json
    Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" -ContentType "application/json" -Body $body
}

$health = Invoke-RestMethod -Uri "$BaseUrl/health/database"
if ($health.status -ne "healthy" -or $health.database -ne "PostgreSQL") {
    throw "Database health did not report healthy PostgreSQL."
}
Add-Pass "Database health" "healthy PostgreSQL"

$swaggerResponse = Invoke-WebRequest -Uri "$BaseUrl/swagger/v1/swagger.json" -UseBasicParsing
$swagger = $swaggerResponse.Content | ConvertFrom-Json
$routeNames = @($swagger.paths.PSObject.Properties.Name)
if ($swaggerResponse.StatusCode -ne 200 -or
    $routeNames -notcontains "/api/auth/login" -or
    $routeNames -notcontains "/api/recommendations/projects") {
    throw "Swagger is unavailable or missing core routes."
}
Add-Pass "Swagger contract" "HTTP 200 with auth and recommendation routes"

if ($StudentCredential) {
    $student = Invoke-Login $StudentCredential
    if ($student.user.role -ne "Student") { throw "Configured Student credential did not return the Student role." }
    $studentHeaders = @{ Authorization = "Bearer $($student.token)" }

    $profile = Invoke-RestMethod -Uri "$BaseUrl/api/profile" -Headers $studentHeaders
    $projects = @(Invoke-RestMethod -Uri "$BaseUrl/api/projects" -Headers $studentHeaders)
    $applications = @(Invoke-RestMethod -Uri "$BaseUrl/api/applications" -Headers $studentHeaders)
    $teams = @(Invoke-RestMethod -Uri "$BaseUrl/api/teams" -Headers $studentHeaders)
    $history = @(Invoke-RestMethod -Uri "$BaseUrl/api/recommendations/history" -Headers $studentHeaders)
    $teammates = @(Invoke-RestMethod -Uri "$BaseUrl/api/recommendations/teammates" -Headers $studentHeaders)

    Add-Pass "Student login and profile" "role Student; profile completeness $($profile.completenessPercent)%"
    Add-Pass "Student read workflows" "$($projects.Count) projects; $($applications.Count) applications; $($teams.Count) teams"
    Add-Pass "Recommendation reads" "$($history.Count) history rows; $($teammates.Count) teammate suggestions"

    $forbidden = Invoke-WebRequest -Uri "$BaseUrl/api/admin/dashboard" -Headers $studentHeaders -SkipHttpErrorCheck -UseBasicParsing
    if ($forbidden.StatusCode -ne 403) { throw "Student admin-dashboard request returned $($forbidden.StatusCode), expected 403." }
    Add-Pass "Student role boundary" "Admin dashboard returned 403"

    if ($GenerateRecommendation) {
        $batch = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/recommendations/projects" -Headers $studentHeaders -ContentType "application/json" -Body "{}"
        if (@($batch.results).Count -eq 0 -or [string]::IsNullOrWhiteSpace($batch.providerStatus)) {
            throw "Recommendation generation returned no results or provider status."
        }
        Add-Pass "Recommendation generation" "$(@($batch.results).Count) results; status $($batch.providerStatus); reused $($batch.reused)"
    }
}

if ($AdminCredential) {
    $admin = Invoke-Login $AdminCredential
    if ($admin.user.role -ne "Admin") { throw "Configured Admin credential did not return the Admin role." }
    $adminHeaders = @{ Authorization = "Bearer $($admin.token)" }

    $dashboard = Invoke-RestMethod -Uri "$BaseUrl/api/admin/dashboard" -Headers $adminHeaders
    $projects = @(Invoke-RestMethod -Uri "$BaseUrl/api/admin/projects" -Headers $adminHeaders)
    $applications = @(Invoke-RestMethod -Uri "$BaseUrl/api/admin/applications" -Headers $adminHeaders)
    $teams = @(Invoke-RestMethod -Uri "$BaseUrl/api/teams" -Headers $adminHeaders)

    Add-Pass "Admin login and dashboard" "$($dashboard.students) students; $($dashboard.projects) projects; $($dashboard.teams) teams"
    Add-Pass "Admin read workflows" "$($projects.Count) projects; $($applications.Count) applications; $($teams.Count) teams"
}

$checks | Format-Table -AutoSize
Write-Host "Smoke verification passed for $BaseUrl" -ForegroundColor Green
