package com.deadmosquitogames;

import android.app.Activity;
import android.content.Context;
import android.graphics.Bitmap;
import android.os.Build;
import android.print.PrintAttributes;
import android.print.PrintDocumentAdapter;
import android.print.PrintManager;
import androidx.annotation.Keep;
import androidx.annotation.RequiresApi;
import androidx.print.PrintHelper;
import android.webkit.WebView;
import android.webkit.WebViewClient;

import com.deadmosquitogames.util.UnityUtil;

@Keep
public class PrintHelperUtils {
	private static WebView mWebView;
	@Keep
	public static void printBitmap(String jobName, Bitmap bitmap, PrintHelper helper) {
		helper.printBitmap(jobName, bitmap, new PrintHelper.OnPrintFinishCallback() {
			@Override
			public void onFinish() {
				UnityUtil.onPrintSuccess();
			}
		});
	}

	@Keep
	@RequiresApi(api = Build.VERSION_CODES.LOLLIPOP)
	public static WebView createWebView(final Activity activity, final String jobName) {
		WebView webView = new WebView(activity);
		webView.setWebViewClient(new WebViewClient() {
			public boolean shouldOverrideUrlLoading(WebView view, String url) {
				return false;
			}

			@Override
			public void onPageFinished(WebView view, String url) {
				createWebPrintJob(view, activity, jobName);
				mWebView = null;
			}
		});

		mWebView = webView;
		return webView;
	}

	@RequiresApi(api = Build.VERSION_CODES.LOLLIPOP)
	private static void createWebPrintJob(WebView webView, Activity activity, String jobName) {
		PrintManager printManager = (PrintManager) activity.getSystemService(Context.PRINT_SERVICE);
		PrintDocumentAdapter printAdapter = webView.createPrintDocumentAdapter(jobName);
		assert printManager != null;
		printManager.print(jobName, printAdapter,
				new PrintAttributes.Builder().build());
	}
}
