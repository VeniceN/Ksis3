using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static List<TcpClient> clients = new();
    static TcpListener server;
    static bool isRunning = true;

    static async Task Main()
    {
        Console.WriteLine("1. Сервер\n2. Клиент");
        string choice = Console.ReadLine();

        if (choice == "1") await StartServer();
        else if (choice == "2") await StartClient();
        else Console.WriteLine("Неверный выбор.");
    }

    // Запуск сервера
    static async Task StartServer()
    {
        Console.Write("Введите порт сервера: ");
        int port = int.Parse(Console.ReadLine());

        if (!Utils.IsPortAvailable(port))
        {
            Console.WriteLine("Ошибка: порт уже используется.");
            return;
        }

        server = new TcpListener(IPAddress.Any, port);
        server.Start();
        Console.WriteLine($"Сервер запущен на {port}. Введите '/stop' для завершения.");

        Task.Run(ServerCommandListener);
        await AcceptClientsAsync();
    }

    // Ожидание клиентов
    static async Task AcceptClientsAsync()
    {
        while (isRunning)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            clients.Add(client);
            _ = HandleClient(client);
        }
    }

    // Обработка сообщений клиента
    static async Task HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        int nameLen = await stream.ReadAsync(buffer, 0, buffer.Length);
        string username = Encoding.UTF8.GetString(buffer, 0, nameLen).Trim();
        Console.WriteLine($"{username} подключился.");

        await BroadcastMessage($"{username} подключился к чату.", null);

        while (true)
        {
            int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (byteCount == 0) break;

            string message = Encoding.UTF8.GetString(buffer, 0, byteCount).Trim();
            if (message == "/exit") break;

            await BroadcastMessage($"{username}: {message}", client);
        }

        clients.Remove(client);
        client.Close();
        Console.WriteLine($"{username} отключился.");
        await BroadcastMessage($"{username} вышел из чата.", null);
    }

    // Отправка сообщений всем клиентам
    static async Task BroadcastMessage(string message, TcpClient sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        foreach (var client in clients)
        {
            if (client != sender)
                await client.GetStream().WriteAsync(data, 0, data.Length);
        }
    }

    // Обработка команд сервера
    static void ServerCommandListener()
    {
        while (true)
        {
            if (Console.ReadLine()?.ToLower() == "/stop") StopServer();
        }
    }

    // Остановка сервера
    static void StopServer()
    {
        isRunning = false;
        foreach (var client in clients) client.Close();
        clients.Clear();
        server.Stop();
        Console.WriteLine("Сервер остановлен.");
    }

    // Запуск клиента
    static async Task StartClient()
    {
        Console.Write("Введите IP сервера: ");
        string ip = Console.ReadLine();
        Console.Write("Введите порт сервера: ");
        int port = int.Parse(Console.ReadLine());
        Console.Write("Введите ваше имя: ");
        string username = Console.ReadLine();

        TcpClient client = new();
        await client.ConnectAsync(ip, port);
        Console.WriteLine("Подключено к серверу (для того, чтобы выйти из чата введите команду /exit).");

        NetworkStream stream = client.GetStream();
        await stream.WriteAsync(Encoding.UTF8.GetBytes(username));

        _ = ReceiveMessages(client);

        while (true)
        {
            string message = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(message)) continue;

            await stream.WriteAsync(Encoding.UTF8.GetBytes(message));
            if (message.ToLower() == "/exit") break;
        }

        client.Close();
        Console.WriteLine("Отключено от сервера.");
    }

    // Получение сообщений от сервера
    static async Task ReceiveMessages(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        while (true)
        {
            int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (byteCount == 0) break;

            Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, byteCount));
        }
    }
}

// Проверка доступности порта
class Utils
{
    public static bool IsPortAvailable(int port)
    {
        try
        {
            TcpListener listener = new(IPAddress.Any, port);
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