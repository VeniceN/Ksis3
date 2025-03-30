using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static List<TcpClient> clients = new List<TcpClient>(); // Список для хранения клиентов

    static async Task Main(string[] args)
    {
        Console.WriteLine("Выберите режим:");
        Console.WriteLine("1. Сервер");
        Console.WriteLine("2. Клиент");

        string choice = Console.ReadLine();

        if (choice == "1")
        {
            await StartServer();
        }
        else if (choice == "2")
        {
            await StartClient();
        }
        else
        {
            Console.WriteLine("Неверный выбор.");
        }
    }

    // Метод для запуска сервера
    static async Task StartServer()
    {
        Console.Write("Введите порт сервера: ");
        int port = int.Parse(Console.ReadLine());

        if (!Utils.IsPortAvailable(port))
        {
            Console.WriteLine("Ошибка: порт уже используется.");
            return;
        }

        TcpListener server = new TcpListener(IPAddress.Any, port);
        server.Start();
        Console.WriteLine($"Сервер запущен на порту {port}...");

        while (true)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            Console.WriteLine("Клиент подключился.");

            clients.Add(client); 

            _ = HandleClient(client); // Обрабатываем клиента асинхронно
        }
    }

    // Метод для обработки клиентов на сервере
    static async Task HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        while (true)
        {
            int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (byteCount == 0) break; 

            string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
            Console.WriteLine($"Сообщение от клиента: {message}");

            // После получения сообщения отправляем его всем остальным клиентам
            await BroadcastMessage(message, client);
        }

        // Удаляем клиента из списка при отключении
        clients.Remove(client);
        client.Close();
    }

    // Метод для отправки сообщений всем подключённым клиентам
    static async Task BroadcastMessage(string message, TcpClient sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);

        foreach (var client in clients)
        {
            if (client != sender) // Не отправляем сообщение тому, кто его отправил
            {
                await client.GetStream().WriteAsync(data, 0, data.Length);
            }
        }
    }

    // Метод для запуска клиента
    static async Task StartClient()
    {
        Console.Write("Введите IP сервера: ");
        string ip = Console.ReadLine();

        Console.Write("Введите порт сервера: ");
        int port = int.Parse(Console.ReadLine());

        TcpClient client = new TcpClient();
        await client.ConnectAsync(ip, port);
        Console.WriteLine("Подключено к серверу.");

        _ = ReceiveMessages(client); // Получение сообщений от сервера

        while (true)
        {
            string message = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(message)) continue;

            byte[] data = Encoding.UTF8.GetBytes(message);
            await client.GetStream().WriteAsync(data, 0, data.Length);
        }
    }

    // Метод для получения сообщений от сервера
    static async Task ReceiveMessages(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        while (true)
        {
            int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (byteCount == 0) break; 

            string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
            Console.WriteLine($"Новое сообщение: {message}");
        }

        Console.WriteLine("Отключено от сервера.");
    }
}

// Проверяет доступность порта
class Utils
{
    public static bool IsPortAvailable(int port)
    {
        try
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
