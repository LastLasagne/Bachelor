package com.deadmosquitogames.goldfinger;

public enum Warning {
    /**
     * The image acquired was good.
     */
    GOOD(0),
    /**
     * Only a partial fingerprint image was detected.
     */
    PARTIAL(1),
    /**
     * The fingerprint image was too noisy to process due to a detected condition.
     */
    INSUFFICIENT(2),
    /**
     * The fingerprint image was too noisy due to suspected or detected dirt on the sensor.
     */
    DIRTY(3),
    /**
     * The fingerprint image was unreadable due to lack of motion.
     */
    TOO_SLOW(4),
    /**
     * The fingerprint image was incomplete due to quick motion.
     */
    TOO_FAST(5),
    /**
     * Fingerprint valid but not recognized.
     */
    FAILURE(6);

    private final int value;

    Warning(int value) {
        this.value = value;
    }

    public int getValue() {
        return value;
    }

    static Warning fromId(int id) {
        switch (id) {
            case 0:
                return GOOD;
            case 1:
                return PARTIAL;
            case 2:
                return INSUFFICIENT;
            case 3:
                return DIRTY;
            case 4:
                return TOO_SLOW;
            case 5:
                return TOO_FAST;
            default:
                return FAILURE;
        }
    }
}
