namespace ClipCore.API.Models.Auth;

public class AuthenticateRequest  { public string Email { get; set; } = ""; public string Password { get; set; } = ""; }
public class AuthenticateResponse { public string Token { get; set; } = ""; public string Email { get; set; } = ""; public string Role { get; set; } = ""; public int? SellerId { get; set; } }
public class RegisterSellerRequest { public string Email { get; set; } = ""; public string Password { get; set; } = ""; public string DisplayName { get; set; } = ""; public string Slug { get; set; } = ""; }
public class ForgotPasswordRequest { public string Email { get; set; } = ""; }
public class ResetPasswordRequest  { public string Email { get; set; } = ""; public string Token { get; set; } = ""; public string Password { get; set; } = ""; }
