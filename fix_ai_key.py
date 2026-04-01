#!/usr/bin/env python3
"""
Re-encrypt the AI API key using the correct encryption key.
The issue is that the API key stored in the database was encrypted with a different
master key than what the backend is currently using.
"""
import base64
import os
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives import padding
from cryptography.hazmat.backends import default_backend

# The encryption key from .env.prod
ENCRYPTION_KEY_B64 = "jr5YmujZG8jOkU3Xw36h5AnekBdE9ZTDF/Ys5DmRIVs="
ENCRYPTION_KEY = base64.b64decode(ENCRYPTION_KEY_B64)

# The OPENAI_API_KEY from environment (Manus proxy key)
API_KEY = os.environ.get("OPENAI_API_KEY", "")

if not API_KEY:
    print("ERROR: OPENAI_API_KEY not set in environment")
    exit(1)

print(f"Encryption key length: {len(ENCRYPTION_KEY)} bytes")
print(f"API key to encrypt: {API_KEY[:10]}...")

# Encrypt using AES-256-CBC with PKCS7 padding (matching the C# implementation)
iv = os.urandom(16)  # Generate random IV

# Pad the plaintext
padder = padding.PKCS7(128).padder()
padded_data = padder.update(API_KEY.encode('utf-8')) + padder.finalize()

# Encrypt
cipher = Cipher(algorithms.AES(ENCRYPTION_KEY), modes.CBC(iv), backend=default_backend())
encryptor = cipher.encryptor()
ciphertext = encryptor.update(padded_data) + encryptor.finalize()

# Prepend IV to ciphertext (matching C# implementation)
result = iv + ciphertext
encrypted_b64 = base64.b64encode(result).decode('utf-8')

print(f"Encrypted API key (base64): {encrypted_b64[:50]}...")
print(f"Full encrypted key: {encrypted_b64}")

# Verify by decrypting
full_cipher = base64.b64decode(encrypted_b64)
extracted_iv = full_cipher[:16]
extracted_cipher = full_cipher[16:]

cipher2 = Cipher(algorithms.AES(ENCRYPTION_KEY), modes.CBC(extracted_iv), backend=default_backend())
decryptor = cipher2.decryptor()
decrypted_padded = decryptor.update(extracted_cipher) + decryptor.finalize()

unpadder = padding.PKCS7(128).unpadder()
decrypted = unpadder.update(decrypted_padded) + unpadder.finalize()

print(f"Verification - Decrypted key: {decrypted.decode('utf-8')[:10]}...")
assert decrypted.decode('utf-8') == API_KEY, "Decryption verification failed!"
print("Verification PASSED!")

# Output the SQL update command
print(f"\n--- SQL Update Command ---")
print(f"UPDATE AiConfigurations SET EncryptedApiKey = '{encrypted_b64}' WHERE Id = 'E2D1C02A-1F8B-408A-9864-018488BA2EDE';")
