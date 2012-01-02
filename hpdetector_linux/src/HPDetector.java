/*
 * HPDetector
 * Version 0.1 (December 8th, 2008)
 * Copyright © 2008 Michael Landi
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

import java.io.*;
import java.net.*;
import java.util.*;

public class HPDetector {
	private final String 		KTCPPATH = "/proc/net/tcp";
	private final String 		KUDPPATH = "/proc/net/udp";
	
	private Vector<Integer>		_vecKTCP;
	private Vector<Integer>		_vecKUDP;
	private Vector<Integer>		_vecBindTCP;
	private Vector<Integer>		_vecBindUDP;
	
	public HPDetector() {
		_vecKTCP = new Vector<Integer>();
		_vecKUDP = new Vector<Integer>();
		_vecBindTCP = new Vector<Integer>();
		_vecBindUDP = new Vector<Integer>();
		
		System.out.println("Hidden Port Detector - Version 0.1");
		System.out.println("Copyright © 2008 Michael Landi (mlandi.design@gmail.com)\n");
	}
	
	private boolean isTCPHidden(int port) {
		for (int intBuffer : _vecKTCP)
			if (intBuffer == port)
				return false;
				
		return true;
	}
	
	private boolean isUDPHidden(int port) {
		for (int intBuffer : _vecKUDP)
			if (intBuffer == port)
				return false;
				
		return true;
	}
	
	private void getBoundTCPPorts() {
		ServerSocket sSocket;
		
		for (int i = 0x1; i < 65535; i++) {
			try {
				sSocket = new ServerSocket(i);
				sSocket.close();
				sSocket = null;
			}
			catch (Exception e) {
				 _vecBindTCP.add(i);
			}
		}
	}
	
	private void getBoundUDPPorts() {
		DatagramSocket dSocket;
		
		for (int i = 0x0; i < 65535; i++) {
			try {
				dSocket = new DatagramSocket(i);
				dSocket.close();
				dSocket = null;
			}
			catch (Exception e) {
				_vecBindUDP.add(i);
			}
		}
	}
	
	private void getKernelPorts(String file, Vector<Integer> list) throws Exception {
		int intCount = 0x0;
		FileReader fReader = new FileReader(file);
		BufferedReader bReader = new BufferedReader(fReader);
		
		String strBuffer = bReader.readLine();
		
		while (strBuffer != null) {
			if (intCount++ != 0x0 && !strBuffer.trim().equals("")) {
				String[] strSplit = strBuffer.trim().split(" ");
				String[] strSplitColon = strSplit[0x1].split(":");
				
				list.add(Integer.parseInt(strSplitColon[0x1], 0x10));
			}
			
			strBuffer = bReader.readLine();
		}
		
		fReader.close();
	}
	
	public void start() {
		System.out.println("Scanning for hidden ports...\n");
		
		try {
			getKernelPorts(KTCPPATH, _vecKTCP);
			getBoundTCPPorts();
			getKernelPorts(KUDPPATH, _vecKUDP);
			getBoundUDPPorts();
			
			int intCount = 0x0;
			
			for (int intBuffer : _vecBindTCP)
				if (isTCPHidden(intBuffer)) {
					intCount++;
					System.out.println("TCP/" + intBuffer + ": may be a hidden port.");
				}
				
			for (int intBuffer : _vecBindUDP)
				if (isUDPHidden(intBuffer)) {
					intCount++;
					System.out.println("UDP/" + intBuffer + ": may be a hidden port.");
				}
				
			if (intCount != 0x0)	
				System.out.println("\n" + intCount + " possible hidden port(s).");
			else
				System.out.println("\nNo hidden ports detected.");
		}
		catch (Exception e) {
			System.out.println("Application failed to execute: " + e.toString());
			System.exit(0x1);
		}
		
		System.exit(0x0);
	}
	
	public static void main(String[] args) {
		new HPDetector().start();
	}
}
