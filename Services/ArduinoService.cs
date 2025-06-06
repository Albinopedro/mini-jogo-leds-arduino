using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;
using miniJogo.Models;

namespace miniJogo.Services
{
    public class ArduinoService : IDisposable
    {
        private SerialPort? _serialPort;
        private bool _isConnected = false;
        private readonly object _lockObject = new();

        public event EventHandler<ArduinoMessageEventArgs>? MessageReceived;
        public event EventHandler<bool>? ConnectionChanged;

        public bool IsConnected
        {
            get
            {
                lock (_lockObject)
                {
                    return _isConnected && _serialPort?.IsOpen == true;
                }
            }
        }

        public string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        public async Task<bool> ConnectAsync(string portName, int baudRate = 9600)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_serialPort?.IsOpen == true)
                    {
                        _serialPort.Close();
                        _serialPort.Dispose();
                    }

                    _serialPort = new SerialPort(portName, baudRate)
                    {
                        ReadTimeout = 500,
                        WriteTimeout = 500
                    };

                    _serialPort.DataReceived += SerialPort_DataReceived;
                    _serialPort.Open();
                    _isConnected = true;
                }

                // Reduced wait time for faster connection
                await Task.Delay(1000);

                ConnectionChanged?.Invoke(this, true);
                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                ConnectionChanged?.Invoke(this, false);
                throw new InvalidOperationException($"Erro ao conectar na porta {portName}: {ex.Message}", ex);
            }
        }

        public void Disconnect()
        {
            lock (_lockObject)
            {
                try
                {
                    _isConnected = false;
                    if (_serialPort?.IsOpen == true)
                    {
                        _serialPort.DataReceived -= SerialPort_DataReceived;
                        _serialPort.Close();
                    }
                    _serialPort?.Dispose();
                    _serialPort = null;
                }
                catch (Exception)
                {
                    // Silent error handling for better performance
                }
                finally
                {
                    ConnectionChanged?.Invoke(this, false);
                }
            }
        }

        public async Task<bool> SendCommandAsync(string command)
        {
            try
            {
                await Task.Run(() =>
                {
                    lock (_lockObject)
                    {
                        if (!_isConnected || _serialPort?.IsOpen != true)
                        {
                            throw new InvalidOperationException("Arduino not connected");
                        }
                        _serialPort.WriteLine(command);
                    }
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> StartGameAsync(GameMode gameMode)
        {
            return await SendCommandAsync($"START_GAME:{(int)gameMode}");
        }

        public async Task<bool> StopGameAsync()
        {
            return await SendCommandAsync("STOP_GAME");
        }

        public async Task<bool> SendKeyPressAsync(int keyIndex)
        {
            return await SendCommandAsync($"KEY_PRESS:{keyIndex}");
        }

        public async Task<bool> ResetScoreAsync()
        {
            return await SendCommandAsync("RESET_SCORE");
        }

        public async Task<bool> RequestStatusAsync()
        {
            return await SendCommandAsync("GET_STATUS");
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data;
                lock (_lockObject)
                {
                    if (!_isConnected || _serialPort?.IsOpen != true) return;
                    data = _serialPort.ReadLine().Trim();
                }

                if (!string.IsNullOrEmpty(data))
                {
                    var message = ParseArduinoMessage(data);
                    MessageReceived?.Invoke(this, new ArduinoMessageEventArgs(message));
                }
            }
            catch (Exception)
            {
                // Silent error handling for better responsiveness
            }
        }

        private ArduinoMessage ParseArduinoMessage(string rawMessage)
        {
            var message = new ArduinoMessage
            {
                RawMessage = rawMessage,
                Timestamp = DateTime.Now
            };

            try
            {
                if (rawMessage.StartsWith("STATUS:"))
                {
                    message.Type = ArduinoMessageType.Status;
                    var parts = rawMessage.Substring(7).Split(',');
                    if (parts.Length >= 4)
                    {
                        if (int.TryParse(parts[0], out int mode)) message.Data["currentMode"] = mode;
                        message.Data["gameActive"] = parts[1] == "1";
                        if (int.TryParse(parts[2], out int score)) message.Data["score"] = score;
                        if (int.TryParse(parts[3], out int level)) message.Data["level"] = level;
                    }
                }
                else if (rawMessage.StartsWith("EVENT:"))
                {
                    message.Type = ArduinoMessageType.Event;
                    var eventData = rawMessage.Substring(6);
                    var parts = eventData.Split(',');
                    
                    message.Data["eventType"] = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        if (int.TryParse(parts[i], out int intValue))
                            message.Data[$"value{i-1}"] = intValue;
                        else
                            message.Data[$"value{i-1}"] = parts[i];
                    }
                }
                else if (rawMessage == "ARDUINO_READY")
                {
                    message.Type = ArduinoMessageType.Ready;
                }
                else
                {
                    message.Type = ArduinoMessageType.Unknown;
                }
            }
            catch (Exception)
            {
                message.Type = ArduinoMessageType.Unknown;
            }

            return message;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }

    public enum ArduinoMessageType
    {
        Status,
        Event,
        Ready,
        Unknown
    }

    public class ArduinoMessage
    {
        public ArduinoMessageType Type { get; set; }
        public string RawMessage { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();

        public T GetData<T>(string key, T defaultValue = default!)
        {
            if (Data.TryGetValue(key, out object? value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }

    public class ArduinoMessageEventArgs : EventArgs
    {
        public ArduinoMessage Message { get; }

        public ArduinoMessageEventArgs(ArduinoMessage message)
        {
            Message = message;
        }
    }
}