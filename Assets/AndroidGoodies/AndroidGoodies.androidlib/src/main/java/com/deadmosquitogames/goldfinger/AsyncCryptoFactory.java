package com.deadmosquitogames.goldfinger;

import androidx.annotation.Nullable;
import androidx.core.hardware.fingerprint.FingerprintManagerCompat;

import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;

class AsyncCryptoFactory {

    private final CryptoFactory cryptoFactory;
    private final ExecutorService executor;
    private Future task;

    AsyncCryptoFactory(CryptoFactory cryptoFactory) {
        this.cryptoFactory = cryptoFactory;
        this.executor = Executors.newSingleThreadExecutor();
    }

    void createCryptoObject(String keyName, Mode mode, AsyncCryptoFactory.Callback callback) {
        if (task != null && !task.isDone()) {
            task.cancel(true);
        }

        this.task = executor.submit(new CryptoObjectInitRunnable(cryptoFactory, keyName, mode, callback));
    }

    static abstract class Callback {

        boolean canceled = false;

        abstract void onCryptoObjectCreated(@Nullable FingerprintManagerCompat.CryptoObject cryptoObject);

        void cancel() {
            canceled = true;
        }
    }
}
