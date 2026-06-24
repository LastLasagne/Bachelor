package com.deadmosquitogames.goldfinger;

import androidx.annotation.Nullable;
import androidx.core.hardware.fingerprint.FingerprintManagerCompat;
import android.util.Base64;

public interface Crypto {

    /**
     * Encrypt value with unlocked cryptoObject. Return null if encryption fails.
     */
    @Nullable
    String encrypt(FingerprintManagerCompat.CryptoObject cryptoObject, String value);

    /**
     * Decrypt value with unlocked cryptoObject. Return null if decryption fails.
     */
    @Nullable
    String decrypt(FingerprintManagerCompat.CryptoObject cryptoObject, String value);

    class Default implements Crypto {

        private final Logger logger;

        Default(Logger logger) {
            this.logger = logger;
        }

        @Nullable
        @Override
        public String encrypt(FingerprintManagerCompat.CryptoObject cryptoObject, String value) {
            try {
                byte[] encryptedBytes = cryptoObject.getCipher().doFinal(value.getBytes());
                return Base64.encodeToString(encryptedBytes, Base64.DEFAULT);
            } catch (Exception e) {
                logger.log(e);
                return null;
            }
        }

        @Nullable
        @Override
        public String decrypt(FingerprintManagerCompat.CryptoObject cryptoObject, String value) {
            try {
                byte[] decodedBytes = Base64.decode(value, Base64.DEFAULT);
                return new String(cryptoObject.getCipher().doFinal(decodedBytes));
            } catch (Exception e) {
                logger.log(e);
                return null;
            }
        }
    }
}
