namespace DeadMosquito.AndroidGoodies
{
	using System;
	using Internal;
	using JetBrains.Annotations;

	/// <summary>
	/// Class to interact with clipboard
	/// </summary>
	public static class AGClipboard
	{
		[PublicAPI]
		public static void SetClipBoardText([NotNull] string label, [NotNull] string text)
		{
			if (label == null)
			{
				throw new ArgumentNullException(nameof(label));
			}

			if (text == null)
			{
				throw new ArgumentNullException(nameof(text));
			}

			AGUtils.RunOnUiThread(() =>
			{
				var clip = C.AndroidContentClipData.AJCCallStaticOnceAJO("newPlainText", label, text);
				AGSystemService.ClipboardService.Call("setPrimaryClip", clip);
			});
		}

		/// <summary>
		/// Get text from the system clipboard
		/// </summary>
		/// <returns>Text from the system clipboard</returns>
		public static string GetClipboardText()
		{
			var clipboard = AGSystemService.ClipboardService;
			var primaryClipDescription = clipboard.CallAJO("getPrimaryClipDescription");
			var hasText = primaryClipDescription.CallBool("hasMimeType", "text/plain");
			if (!clipboard.IsJavaNull() && hasText)
			{
				var item = clipboard.CallAJO("getPrimaryClip").CallAJO("getItemAt", 0);
				return item.CallAJO("getText").JavaToString();
			}

			return string.Empty;
		}
	}
}
