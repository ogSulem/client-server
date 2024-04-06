using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

class Server
{
    static Dictionary<int, string> idToName = new Dictionary<int, string>();
    static string source = "./server/data/";
    static string dirClient = "./client/data/";

    static void Main(string[] args)
    {
        StartServerAsync().GetAwaiter().GetResult();
    }

    static async Task StartServerAsync()
    {
        IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, 8888);
        using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(ipPoint);
        socket.Listen();
        Console.WriteLine("Server started!\n");
        RefreshIds();

        while (true)
        {
            Socket client = await socket.AcceptAsync();
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    static async Task HandleClientAsync(Socket client)
    {
        Console.WriteLine($"Подключен клиент: {client.RemoteEndPoint}");

        string request = string.Empty;
        using (StreamReader reader = new StreamReader(new NetworkStream(client)))
        { 
            request = await reader.ReadLineAsync();
            Console.WriteLine("Запрос: " + request);
        }

        using (StreamWriter writer = new StreamWriter(new NetworkStream(client)))
        {
            string response = HandleRequest(request);
            await writer.WriteLineAsync(response);
            Console.WriteLine("Ответ отправлен: " + response + "\n");
        }
    }
    static string HandleRequest(string request)
    {
        string[] parts = request.Split(' ');
        string action = parts[0];

        if (action == "PUT")
        {
            string data = string.Empty;
            for (int i = 2; i < parts.Length; i++) data += parts[i] + " ";
            return HandlePut(parts[1], data);
        }
        else if (action == "GET")
        {
            string filename = parts[2];
            if (parts[1] == "2") filename = GetName(int.Parse(filename));
            return HandleGet(filename);
        }
        else if (action == "DELETE")
        {
            string filename = parts[2];
            if (parts[1] == "2") filename = GetName(int.Parse(filename));
            return HandleDelete(filename);
        }
        else if (action == "SAVE")
        {
            return HandleSave(parts[1], parts[2], parts[3]);
        }
        else if (action == "EXIT")
        {
            SaveId();
            return "777";
        }
        else
        {
            return "Invalid action";
        }
    }

    static string HandlePut(string filename, string data)
    {
        if (!File.Exists(source + filename))
        {
            File.WriteAllText(source + filename, data);
            idToName[idToName.Count + 1] = filename;
            return $"200 {idToName.Count}";
        }
        else
        {
            return "403";
        }
    }

    static string HandleGet(string filename)
    {
        string filePath = Path.Combine(source, filename);
        if (File.Exists(filePath))
        {
            try
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        fileStream.CopyTo(memoryStream);

                        byte[] fileBytes = memoryStream.ToArray();
                        return $"200 {filename} {Convert.ToBase64String(fileBytes)}";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while reading file: {ex.Message}");
                return "500"; 
            }
        }
        else
        {
            return "404";
        }
    }

    static string HandleDelete(string filename)
    {
        if (File.Exists(source + filename))
        {
            File.Delete(source + filename);
            DeleteID(filename);
            return "200";
        }

        else
        {
            return "404";
        }
    }

    static string HandleSave(string nameOnClient, string nameOnServer, string data)
    {
        if (nameOnServer == "untitled") nameOnServer = $"file{idToName.Count + 1}.txt";
        if (File.Exists(dirClient + nameOnClient))
        {
            SaveFile(data, nameOnServer);
            idToName[idToName.Count + 1] = nameOnServer;
            return $"200 {idToName.Count}";
        }
        else
        {
            return "404";
        }
    }

    static string GetName(int id)
    {
        return idToName[id];
    }

    static void DeleteID(string id)
    {
        foreach (int key in idToName.Keys)
        {
            if (idToName[key] == id)
            {
                idToName.Remove(key);
                break;
            }
        }
    }

    static async void RefreshIds()
    {
        using (StreamReader reader = new StreamReader("./server/data/dict.txt"))
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                idToName[int.Parse(line.Split()[0])] = line.Split()[1];
            }
        }
    }

    static void SaveId()
    {
        using (StreamWriter w = new StreamWriter("./server/data/dict.txt", false))
        {
            foreach (int s in idToName.Keys)
            {
                w.WriteLine($"{s} {idToName[s]}");
            }
            w.Close();
        }
    }

    static void SaveFile(string base64String, string filename)
    {
        try
        {
            byte[] fileBytes = Convert.FromBase64String(base64String);
            string filePath = Path.Combine(source, filename);
            File.WriteAllBytes(source + filename, fileBytes);
            Console.WriteLine($"Файл успешно сохранен: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении файла: {ex.Message}");
        }
    }
}