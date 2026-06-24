package com.deadmosquitogames;

import java.io.BufferedReader;
import java.io.FileReader;
import java.io.IOException;

@SuppressWarnings("unused")
public class UnityHelperUtils {
	public static String loadFileAsString(String filePath) throws java.io.IOException {
		StringBuilder data = new StringBuilder(1000);
		BufferedReader reader = new BufferedReader(new FileReader(filePath));
		char[] buf = new char[1024];
		int numRead;
		while ((numRead = reader.read(buf)) != -1) {
			String readData = String.valueOf(buf, 0, numRead);
			data.append(readData);
		}
		reader.close();
		return data.toString();
	}

	public static String getMacAddress() {
		try {
			return loadFileAsString("/sys/class/net/eth0/address")
					.toUpperCase().substring(0, 17);
		} catch (IOException e) {
			e.printStackTrace();
			return null;
		}
	}
	
	
    public static Class<?> classForName(String clazz) {
	    try {
	        return Class.forName(clazz);
	    } catch (ClassNotFoundException e) {
	        e.printStackTrace();
	        return null;
	    }
	}
}
