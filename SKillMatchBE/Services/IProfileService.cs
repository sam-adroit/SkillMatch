using SkillMatchBE.DTOs.Profiles;

namespace SkillMatchBE.Services;

public interface IProfileService
{
    Task<StudentProfileResponse?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<ProfileServiceResult> UpdateAsync(Guid userId, UpdateStudentProfileRequest request, CancellationToken cancellationToken);
}

public enum ProfileFailure { None, InvalidExperienceLevel, InvalidLookup }

public sealed record ProfileServiceResult(StudentProfileResponse? Profile, ProfileFailure Failure)
{
    public static ProfileServiceResult Success(StudentProfileResponse profile) => new(profile, ProfileFailure.None);
    public static ProfileServiceResult Failed(ProfileFailure failure) => new(null, failure);
}
