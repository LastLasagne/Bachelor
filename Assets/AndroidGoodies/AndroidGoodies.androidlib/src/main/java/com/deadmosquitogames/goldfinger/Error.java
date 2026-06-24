package com.deadmosquitogames.goldfinger;

public enum Error {
    /**
     * The hardware is unavailable.
     */
    UNAVAILABLE(0),
    /**
     * Error state returned when the sensor was unable to process the current image.
     */
    UNABLE_TO_PROCESS(1),
    /**
     * Error state returned when the current request has been running too long.
     */
    TIMEOUT(2),
    /**
     * Error state returned for operations like enrollment; the operation cannot be completed because there's not
     * enough storage remaining to complete the operation.
     */
    NOT_ENOUGH_SPACE(3),
    /**
     * The operation was canceled because the fingerprint sensor is unavailable.
     */
    CANCELED(3),
    /**
     * The operation was canceled because the API is locked out due to too many attempts.
     */
    LOCKOUT(4),
    /**
     * CryptoFactory failed to initialize CryptoObject.
     */
    CRYPTO_OBJECT_INIT(5),
    /**
     * Crypto failed to decrypt the value.
     */
    DECRYPTION_FAILED(6),
    /**
     * Crypto failed to encrypt the value.
     */
    ENCRYPTION_FAILED(7),
    /**
     * Unknown error happened.
     */
    UNKNOWN(8);

    private final int value;

    Error(int value) {
        this.value = value;
    }

    public int getValue() {
        return value;
    }

    static Error fromId(int id) {
        switch (id) {
            case 1:
                return UNAVAILABLE;
            case 2:
                return UNABLE_TO_PROCESS;
            case 3:
                return TIMEOUT;
            case 4:
                return NOT_ENOUGH_SPACE;
            case 5:
                return CANCELED;
            case 7:
                return LOCKOUT;
            default:
                return UNKNOWN;
        }
    }
}
