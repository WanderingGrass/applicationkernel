using Microsoft.AspNetCore.Authorization;

namespace Todd.ApplicationKernel.Auth;

public class AuthAttribute : AuthorizeAttribute
{
    public AuthAttribute(string scheme, string policy = "") : base(policy)
    {
        AuthenticationSchemes = scheme;
    }
}
