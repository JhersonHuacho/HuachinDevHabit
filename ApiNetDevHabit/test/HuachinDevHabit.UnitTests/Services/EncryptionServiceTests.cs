using HuachinDevHabit.Api.Services.Encryption;
using HuachinDevHabit.Api.Settings;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace HuachinDevHabit.UnitTests.Services
{
	public sealed class EncryptionServiceTests
	{
		private readonly EncryptionService _encryptionService;

		public EncryptionServiceTests()
		{
			IOptions<EncryptionOptions> options = Options.Create(new EncryptionOptions
			{
				Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
			});

			_encryptionService = new EncryptionService(options);
		}

		[Fact]
		public void Decrypt_ShouldReturnPlainText_WhenDecryptingCorrectCiphertext()
		{
			// Arrange
			const string plainText = "sensitive data";
			string cipherText = _encryptionService.Encrypt(plainText);

			// Act
			string decryptedCiphertext = _encryptionService.Decrypt(cipherText);

			// Assert
			Assert.Equal(plainText, decryptedCiphertext);
		}
	}
}
