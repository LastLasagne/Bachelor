using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Storage;
using UnityEngine;

public static class FirebaseGameServices
{
    private static Task initializationTask;
    private static Task<FirebaseUser> signInTask;

    public static FirebaseAuth Auth { get; private set; }
    public static FirebaseFirestore Firestore { get; private set; }
    public static FirebaseStorage Storage { get; private set; }

    public static async Task EnsureInitializedAsync()
    {
        if (initializationTask == null)
        {
            initializationTask = InitializeAsync();
        }

        await initializationTask;
    }

    public static async Task<FirebaseUser> EnsureSignedInAnonymouslyAsync()
    {
        await EnsureInitializedAsync();

        if (Auth.CurrentUser != null)
        {
            return Auth.CurrentUser;
        }

        if (signInTask == null)
        {
            signInTask = SignInAnonymouslyAsync();
        }

        return await signInTask;
    }

    private static async Task InitializeAsync()
    {
        DependencyStatus dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus != DependencyStatus.Available)
        {
            throw new InvalidOperationException($"Could not resolve Firebase dependencies: {dependencyStatus}");
        }

        Auth = FirebaseAuth.DefaultInstance;
        Firestore = FirebaseFirestore.DefaultInstance;
        Storage = FirebaseStorage.DefaultInstance;

        Debug.Log("Firebase initialized.");
    }

    private static async Task<FirebaseUser> SignInAnonymouslyAsync()
    {
        AuthResult result = await Auth.SignInAnonymouslyAsync();
        Debug.Log($"Firebase anonymous sign-in succeeded. User ID: {result.User.UserId}");
        return result.User;
    }
}


