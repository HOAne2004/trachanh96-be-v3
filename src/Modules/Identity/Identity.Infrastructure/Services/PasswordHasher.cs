using Identity.Application.Interfaces;

namespace Identity.Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            // Sử dụng BCrypt để băm mật khẩu
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool Verify(string password, string providedPassword)
        {
            try
            {
                // So sánh mật khẩu đã băm với mật khẩu gốc
                return BCrypt.Net.BCrypt.Verify( providedPassword, password);
            }
            catch (Exception)
            {
                return false;
            }

        }

    }
}
