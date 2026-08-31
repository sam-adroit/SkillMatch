using SkillMatchBE.DTOs.Catalog;
using SkillMatchBE.DTOs.Profiles;
using SkillMatchBE.Entities;
using SkillMatchBE.Repositories;

namespace SkillMatchBE.Services;

public sealed class ProfileService(
    IProfileRepository profiles,
    ILookupRepository lookups,
    IUserRepository users,
    IClock clock) : IProfileService
{
    public async Task<StudentProfileResponse?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetAsync(userId, cancellationToken);
        if (profile is not null) return Map(profile);

        var user = await users.FindByIdAsync(userId, cancellationToken);
        return user is null
            ? null
            : new StudentProfileResponse(user.Id, user.FirstName, user.LastName, user.Email, string.Empty, string.Empty, [], [], [], 0,
                ["Experience level", "Goals", "Preferred technologies", "Skills", "Interests"], null);
    }

    public async Task<ProfileServiceResult> UpdateAsync(
        Guid userId,
        UpdateStudentProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ExperienceLevel>(request.ExperienceLevel, true, out var level))
            return ProfileServiceResult.Failed(ProfileFailure.InvalidExperienceLevel);

        var skillIds = request.SkillIds.Distinct().ToArray();
        var interestIds = request.InterestIds.Distinct().ToArray();
        if (!await lookups.SkillsExistAsync(skillIds, cancellationToken) ||
            !await lookups.InterestsExistAsync(interestIds, cancellationToken))
            return ProfileServiceResult.Failed(ProfileFailure.InvalidLookup);

        var profile = await profiles.GetAsync(userId, cancellationToken);
        if (profile is null)
        {
            profile = new StudentProfile
            {
                UserId = userId,
                Goals = string.Empty,
                User = (await users.FindByIdAsync(userId, cancellationToken))!
            };
        }

        profile.ExperienceLevel = level;
        profile.Goals = request.Goals.Trim();
        profile.PreferredTechnologies = request.PreferredTechnologies
            .Select(item => item.Trim()).Where(item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        profile.UpdatedAt = clock.UtcNow;
        foreach (var profileSkill in profile.Skills.Where(item => !skillIds.Contains(item.SkillId)).ToArray())
            profile.Skills.Remove(profileSkill);
        foreach (var skillId in skillIds.Where(id => profile.Skills.All(item => item.SkillId != id)))
            profile.Skills.Add(new StudentProfileSkill { ProfileUserId = userId, SkillId = skillId });
        foreach (var profileInterest in profile.Interests.Where(item => !interestIds.Contains(item.InterestId)).ToArray())
            profile.Interests.Remove(profileInterest);
        foreach (var interestId in interestIds.Where(id => profile.Interests.All(item => item.InterestId != id)))
            profile.Interests.Add(new StudentProfileInterest { ProfileUserId = userId, InterestId = interestId });

        await profiles.SaveAsync(profile, cancellationToken);
        return ProfileServiceResult.Success((await GetAsync(userId, cancellationToken))!);
    }

    private static StudentProfileResponse Map(StudentProfile profile)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(profile.Goals)) missing.Add("Goals");
        if (profile.PreferredTechnologies.Length == 0) missing.Add("Preferred technologies");
        if (profile.Skills.Count == 0) missing.Add("Skills");
        if (profile.Interests.Count == 0) missing.Add("Interests");
        var completeness = (5 - missing.Count) * 20;

        return new StudentProfileResponse(
            profile.UserId,
            profile.User.FirstName,
            profile.User.LastName,
            profile.User.Email,
            profile.ExperienceLevel.ToString(),
            profile.Goals,
            profile.PreferredTechnologies,
            profile.Skills.Select(item => new LookupResponse(item.SkillId, item.Skill.Name)).OrderBy(item => item.Name).ToArray(),
            profile.Interests.Select(item => new LookupResponse(item.InterestId, item.Interest.Name)).OrderBy(item => item.Name).ToArray(),
            completeness,
            missing,
            profile.UpdatedAt);
    }
}
