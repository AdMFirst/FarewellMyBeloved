using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FarewellMyBeloved.Services
{
    /// <summary>
    /// Service for managing OAuth state parameters to prevent CSRF attacks
    /// </summary>
    public class StateParameterService : IStateParameterService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<StateParameterService> _logger;
        private const string StateParameterKeyPrefix = "OAuthState_";

        public StateParameterService(IHttpContextAccessor httpContextAccessor, ILogger<StateParameterService> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generates a new cryptographically secure state parameter
        /// </summary>
        /// <returns>A unique state parameter string</returns>
        public string GenerateStateParameter()
        {
            var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            var state = Convert.ToBase64String(bytes);
            _logger.LogInformation("Generated new OAuth state parameter");
            return state;
        }

        /// <summary>
        /// Stores a state parameter for later validation
        /// </summary>
        /// <param name="state">The state parameter to store</param>
        /// <returns>Task representing the async operation</returns>
        public async Task StoreStateParameterAsync(string state)
        {
            if (string.IsNullOrEmpty(state))
                throw new ArgumentException("State parameter cannot be null or empty", nameof(state));

            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                throw new InvalidOperationException("HttpContext or Session is not available");
            }

            await session.LoadAsync();
            session.SetString(StateParameterKeyPrefix + state, state);
            await session.CommitAsync();
            _logger.LogInformation("Stored OAuth state parameter for validation");
        }

        /// <summary>
        /// Validates a state parameter against the stored value
        /// </summary>
        /// <param name="state">The state parameter to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public async Task<bool> ValidateStateParameterAsync(string state)
        {
            if (string.IsNullOrEmpty(state))
            {
                _logger.LogWarning("OAuth state parameter validation failed: state parameter is null or empty");
                return false;
            }

            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                _logger.LogWarning("OAuth state parameter validation failed: HttpContext or Session is not available");
                return false;
            }

            await session.LoadAsync();
            var storedState = session.GetString(StateParameterKeyPrefix + state);
            
            var isValid = !string.IsNullOrEmpty(storedState) && storedState == state;
            
            if (isValid)
            {
                _logger.LogInformation("OAuth state parameter validation successful");
            }
            else
            {
                _logger.LogWarning("OAuth state parameter validation failed: invalid or missing state parameter");
            }
            
            return isValid;
        }

        /// <summary>
        /// Removes a state parameter after successful validation
        /// </summary>
        /// <param name="state">The state parameter to remove</param>
        /// <returns>Task representing the async operation</returns>
        public async Task RemoveStateParameterAsync(string state)
        {
            if (string.IsNullOrEmpty(state))
                return;

            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
                return;

            await session.LoadAsync();
            session.Remove(StateParameterKeyPrefix + state);
            await session.CommitAsync();
            _logger.LogInformation("Removed OAuth state parameter after successful validation");
        }
    }
}