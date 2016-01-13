﻿using System;
using System.IO;

using Java.Lang;

using Android.Bluetooth;
using Android.Util;

namespace Tetrim
{
	/// This thread runs during a connection with a remote device.
	/// It handles all incoming and outgoing transmissions.
	public class ConnectedThread : Thread
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private BluetoothSocket _socket;
		private Stream _inStream;
		private Stream _outStream;
		private BluetoothManager _service;
		private bool _continue = true;
		private bool _end = false;

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public ConnectedThread(BluetoothSocket socket, BluetoothManager service)
		{
			Log.Debug(BluetoothManager.Tag, "Create ConnectedThread: ");
			_socket = socket;
			_service = service;
			Stream tempIn = null;
			Stream tempOut = null;

			// Get the BluetoothSocket input and output streams
			try
			{
				tempIn = socket.InputStream;
				tempOut = socket.OutputStream;
			}
			catch(Java.IO.IOException e)
			{
				Log.Error(BluetoothManager.Tag, "Temp sockets not created", e);
			}

			_inStream = tempIn;
			_outStream = tempOut;
		}

		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
		public override void Run()
		{
			Log.Info(BluetoothManager.Tag, "BEGIN ConnectedThread");
			Name = "ConnectedThread";
			byte[] buffer = new byte[Constants.SizeMaxBluetoothMessage];
			int bytes;

			// Keep listening to the InputStream while connected
			while(_continue)
			{
				try
				{
					// Read from the InputStream
					bytes = _inStream.Read(buffer, 0, buffer.Length);

					if(_continue)
					{
						// Send the obtained bytes to the UI Activity
						_service.ObtainMessage((int) BluetoothManager.MessageType.Read, bytes, -1, buffer).SendToTarget();
					}
				}
				catch(Java.IO.IOException e)
				{
					if(_continue)
					{
						Log.Error(BluetoothManager.Tag, "Disconnected because read didn't success, message=" + e.Message);
						_service.ConnectionLost(null);
						break;
					}
				}
			}
			_end = true;
   		}

		public void Cancel()
		{
			_continue = false;

			if(_socket != null)
			{
				try
				{
					_socket.Close();

					// Wait for the end of the thread
					while(!_end)
					{
						Thread.Sleep(10);
					}
				}
				catch(Java.IO.IOException e)
				{
					Log.Error(BluetoothManager.Tag, "Close() of connected socket failed, message = " + e.Message);
				}
			}
		}

		/// Write to the connected OutStream.
		public void Write(byte[] buffer)
		{
			try
			{
				_outStream.Write(buffer, 0, buffer.Length);

				if(_continue)
				{
					// Share the sent message back to the UI Activity
					_service.ObtainMessage((int) BluetoothManager.MessageType.Write, -1, -1, buffer).SendToTarget();
				}
			}
			catch(Java.IO.IOException e)
			{

				if(_continue)
				{
					Log.Error(BluetoothManager.Tag, "Disconnected because write didn't success, message=" + e.Message);
					_service.ConnectionLost(buffer);
				}
			}
		}
	}
}

