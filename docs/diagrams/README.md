# SkillMatch diagrams

These diagrams describe the current implementation. Mermaid source
is authoritative; the adjacent SVG files are generated presentation artifacts.

| Diagram | Source | Export |
|---|---|---|
| Runtime and layered architecture | `system-architecture.mmd` | `system-architecture.svg` |
| Domain / ER model | `domain-model.mmd` | `domain-model.svg` |
| Apply-to-project sequence | `apply-sequence.mmd` | `apply-sequence.svg` |
| Project-recommendation sequence | `recommendation-sequence.mmd` | `recommendation-sequence.svg` |
| Collaboration / communication | `collaboration-communication.mmd` | `collaboration-communication.svg` |
| Apply-to-project VOPC | `vopc-apply-to-project.mmd` | `vopc-apply-to-project.svg` |
| Project-recommendation VOPC | `vopc-project-recommendation.mmd` | `vopc-project-recommendation.svg` |

Regenerate all SVG exports from the repository root:

```powershell
Get-ChildItem .\docs\diagrams\*.mmd | ForEach-Object {
    npx --yes @mermaid-js/mermaid-cli -i $_.FullName -o ($_.FullName -replace '\.mmd$', '.svg') -b transparent
}
```

Relationship notes:

- `RecommendationHistory.TargetId` is a logical target identifier, not a database
  foreign key, because the table supports a recommendation type discriminator.
- One active course/project cycle is implicit; there is no `Course` or `Cycle` entity.
- Team leadership is a foreign key to `ApplicationUser`, and the service additionally
  requires the leader to be included in `TeamMember`.
- OpenAI generates explanations for deterministic project ranks. Teammate scoring and
  team skill-gap comparison remain deterministic.
