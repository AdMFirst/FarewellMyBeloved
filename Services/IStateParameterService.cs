using System.Threading.Tasks;

namespace FarewellMyBeloved.Services
{
    /// <summary>
    /// Service for managing OAuth state parameters to prevent CSRF attacks
    /// </summary>
    public interface IStateParameterService
    {
        /// <summary>
        /// Generates a new cryptographically secure state parameter
        /// </summary>
        /// <returns>A unique state parameter string</returns>
        string GenerateStateParameter();

        /// <summary>
        /// Stores a state parameter for later validation
        /// </summary>
        /// <param name="state">The state parameter to store</param>
        /// <returns>Task representing the async operation</returns>
        Task StoreStateParameterAsync(string state);

        /// <summary>
        /// Validates a state parameter against the stored value
        /// </summary>
        /// <param name="state">The state parameter to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        Task<bool> ValidateStateParameterAsync(string state);

        /// <summary>
        /// Removes a state parameter after successful validation
        /// </summary>
        /// <param name="state">The state parameter to remove</param>
        /// <returns>Task representing the async operation</returns>
        Task RemoveStateParameterAsync(string state);
    }
}