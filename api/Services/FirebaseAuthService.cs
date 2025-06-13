using FirebaseAuth = Firebase.Auth;

namespace api.Services
{
    public class FirebaseAuthService
    {
        private readonly FirebaseAuth.FirebaseAuthProvider _authProvider;
        private readonly ILogger<FirebaseAuthService> _logger;

        public FirebaseAuthService(IConfiguration configuration, ILogger<FirebaseAuthService> logger)
        {
            var apiKey = configuration["Firebase:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration));
            _authProvider = new FirebaseAuth.FirebaseAuthProvider(new FirebaseAuth.FirebaseConfig(apiKey));
            _logger = logger;
        }

        public async Task<(string Token, string RefreshToken, string FirebaseUid)> RegisterUserAsync(string email, string password, string role)
        {
            try
            {
                var auth = await _authProvider.CreateUserWithEmailAndPasswordAsync(email, password);

                // Set custom claims for the user's role
                var claims = new Dictionary<string, object>
                {
                    { "role", role }
                };

                await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance
                    .SetCustomUserClaimsAsync(auth.User.LocalId, claims);

                // Get a new token that includes the custom claims
                auth = await _authProvider.SignInWithEmailAndPasswordAsync(email, password);
                return (auth.FirebaseToken, auth.RefreshToken, auth.User.LocalId);
            }
            catch (FirebaseAuth.FirebaseAuthException ex) when (ex.Reason == FirebaseAuth.AuthErrorReason.EmailExists)
            {
                // If user already exists in Firebase, try to authenticate instead
                var auth = await _authProvider.SignInWithEmailAndPasswordAsync(email, password);
                return (auth.FirebaseToken, auth.RefreshToken, auth.User.LocalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user with Firebase: {Email}", email);
                throw;
            }
        }

        public async Task<(string Token, string RefreshToken, string FirebaseUid)> LoginUserAsync(string email, string password)
        {
            try
            {
                var auth = await _authProvider.SignInWithEmailAndPasswordAsync(email, password);
                return (auth.FirebaseToken, auth.RefreshToken, auth.User.LocalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging in user with Firebase: {Email}", email);
                throw;
            }
        }

        public async Task<string> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var auth = await _authProvider.RefreshAuthAsync(new FirebaseAuth.FirebaseAuthLink(_authProvider,
                    new FirebaseAuth.FirebaseAuth
                    {
                        RefreshToken = refreshToken
                    }));
                return auth.FirebaseToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing Firebase token");
                throw;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid Firebase token");
                return false;
            }
        }

        public async Task<bool> SetUserRoleAsync(string firebaseUid, string role)
        {
            try
            {
                var claims = new Dictionary<string, object>
                {
                    { "role", role }
                };

                await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance
                    .SetCustomUserClaimsAsync(firebaseUid, claims);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting user role: {FirebaseUid}", firebaseUid);
                return false;
            }
        }

        public async Task<bool> UpdateEmailInFirebaseAsync(string firebaseUid, string newEmail)
        {
            try
            {
                var userUpdateArgs = new FirebaseAdmin.Auth.UserRecordArgs()
                {
                    Uid = firebaseUid,
                    Email = newEmail,
                    EmailVerified = false // Reset email verification when email is changed
                };

                await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.UpdateUserAsync(userUpdateArgs);
                _logger.LogInformation("Email updated successfully in Firebase for user: {FirebaseUid}", firebaseUid);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email in Firebase for user: {FirebaseUid}", firebaseUid);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            try
            {
                await _authProvider.SendPasswordResetEmailAsync(email);
                _logger.LogInformation("Password reset email sent successfully to: {Email}", email);
                return true;
            }
            catch (FirebaseAuth.FirebaseAuthException ex) when (ex.Reason == FirebaseAuth.AuthErrorReason.UserNotFound)
            {
                _logger.LogWarning("Password reset attempted for non-existent email: {Email}", email);
                // Return true to avoid revealing if email exists for security reasons
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to: {Email}", email);
                return false;
            }
        }
    }
}