namespace Superdev.AspNetCore.Infrastructure.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class AuthorizeClaimAttribute : AuthorizeAttribute
    {
        public const string PolicyName = nameof(AuthorizeClaimAttribute);

        /// <summary>
        /// Checks for the existence of a claim.
        /// </summary>
        public AuthorizeClaimAttribute(string claimType)
            : this(ClaimRequirementType.Exists, claimType, Array.Empty<object>())
        {
        }

        /// <summary>
        /// Checks for the existence of a claim with a specific value.
        /// </summary>
        public AuthorizeClaimAttribute(string claimType, string value)
            : this(ClaimRequirementType.All, claimType, new string[] { value })
        {
        }

        /// <summary>
        /// Checks for the existence of a claim with a specific value.
        /// </summary>
        public AuthorizeClaimAttribute(string claimType, char value)
            : this(ClaimRequirementType.All, claimType, new object[] { value })
        {
        }

        public AuthorizeClaimAttribute(ClaimRequirementType requirementType, string claimType, params char[] values)
            : this(requirementType, claimType, values.Cast<object>().ToArray())
        {
        }

        public AuthorizeClaimAttribute(ClaimRequirementType requirementType, string claimType, params string[] values)
            : this(requirementType, claimType, (object[])values)
        {
        }

        private AuthorizeClaimAttribute(ClaimRequirementType requirementType, string claimType, object[] values)
            : base(PolicyName)
        {
            if (string.IsNullOrWhiteSpace(claimType))
            {
                throw new ArgumentNullException(nameof(claimType));
            }

            this.ClaimType = claimType;
            this.RequirementType = requirementType;
            this.Values = values;
        }

        public string ClaimType { get; }

        public ClaimRequirementType RequirementType { get; }

        public object[] Values { get; }
    }
}