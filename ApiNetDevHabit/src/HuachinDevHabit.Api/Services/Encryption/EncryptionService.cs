using HuachinDevHabit.Api.Settings;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace HuachinDevHabit.Api.Services.Encryption
{
	public sealed class EncryptionService
	{
		//private readonly EncryptionOptions _options;
		private readonly byte[] _masterKey;
		private const int IvSize = 16;

		public EncryptionService(IOptions<EncryptionOptions> options)
		{
			//_options = options.Value;
			_masterKey = Convert.FromBase64String(options.Value.Key); // Initialize _masterKey in the constructor
		}

		public string Encrypt(string plainText)
		{
			try
			{
				using var aes = Aes.Create();
				aes.Mode = CipherMode.CBC;
				aes.Padding = PaddingMode.PKCS7;
				aes.Key = _masterKey;
				aes.IV = RandomNumberGenerator.GetBytes(IvSize);

				using var memoryStream = new MemoryStream();
				memoryStream.Write(aes.IV, 0, IvSize);

				using (ICryptoTransform encryptor = aes.CreateEncryptor())
				using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
				using (var streamWriter = new StreamWriter(cryptoStream))
				{
					streamWriter.Write(plainText);
				}

				return Convert.ToBase64String(memoryStream.ToArray());
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Encryption failed", ex);
			}
		}

		public string Decrypt(string cipherText)
		{
			try
			{
				byte[] cipherData = Convert.FromBase64String(cipherText);

				if (cipherData.Length < IvSize)
				{
					throw new ArgumentException("Invalid cipher text format.");
				}

				// Extract the IV and cipher text data from the cipher data
				byte[] iv = new byte[IvSize];
				byte[] encryptedData = new byte[cipherData.Length - IvSize];

				Buffer.BlockCopy(cipherData, 0, iv, 0, IvSize);
				Buffer.BlockCopy(cipherData, IvSize, encryptedData, 0, encryptedData.Length);

				using var aes = Aes.Create();
				aes.Mode = CipherMode.CBC;
				aes.Padding = PaddingMode.PKCS7;
				aes.Key = _masterKey;
				aes.IV = iv;

				using var memoryStream = new MemoryStream(encryptedData);
				using (ICryptoTransform decryptor = aes.CreateDecryptor())
				using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
				using (var streamReader = new StreamReader(cryptoStream))
				{
					return streamReader.ReadToEnd();
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Decryption failed", ex);
			}
		}
	}
}
