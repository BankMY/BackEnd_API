using APIBankingDiplom.DBClasses.DBModels;

namespace APIBankingDiplom.Security
{
    public class UserValidationResult
    {
        #region Properties
        public object? ErrorObject { get; }
        public UserModel? User { get; }
        public bool IsValid => ErrorObject == null;
        #endregion
        #region Constructors
        private UserValidationResult(UserModel user)
        {
            User = user;
        }
        private UserValidationResult(object error)
        {
            ErrorObject = error;
        }
        #endregion
        #region Factory Methods
        public static UserValidationResult Success(UserModel user)
        {
            return new UserValidationResult(user);
        }

        public static UserValidationResult Failure(object error)
        {
            return new UserValidationResult(error);
        }
        #endregion
    }
}
