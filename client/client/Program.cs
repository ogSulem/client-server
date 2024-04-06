using System.Net.Sockets;

class Client
{
    static async Task Main(string[] args)
    {
        await StartClientAsync();
    }

    static async Task StartClientAsync()
    {
        string directoryPath = "./client/data/";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        while (true)
        {
            Console.Write("1 - Создать файл на сервере\n" +
                   "2 - Получить файл с сервера\n" +
                   "3 - Удалить файл на сервере\n" +
                   "4 - Сохранить на сервере\n" +
                   "exit- Отмена\nВаш ввод: ");
            string choose = Console.ReadLine();
            while (choose != "1" && choose != "2" && choose != "3" && choose != "exit" && choose != "4")
            {
                Console.Write("Неправильно!\nВведите еще раз: ");
                choose = Console.ReadLine();
            }

            string request = string.Empty;

            if (choose.ToLower() == "exit")
            {
                request = "EXIT";
                choose = "5";
            }

            string input = String.Empty;
            string filename = string.Empty;
            switch (int.Parse(choose))
            {
                case 1:
                    Console.Write("Введите название файла: ");
                    filename = Console.ReadLine();
                    Console.Write("Введите содержимое файла: ");
                    string data = Console.ReadLine();
                    request = $"PUT {filename} {data}";
                    break;
                case 2:
                    Console.Write("1 - по имени\n2 - по ID\nВвод: ");
                    input = Console.ReadLine();
                    while (input != "1" && input != "2") 
                    {
                        Console.Write("Не верно! Еще раз: ");
                        input = Console.ReadLine();
                    }
                    if (input == "1")
                    {
                        Console.Write("Введите название файла: ");
                        filename = Console.ReadLine();
                    } else
                    {
                        Console.Write("Введите ID: ");
                        filename = Console.ReadLine();
                    }
                    request = $"GET {input} {filename}";
                    break;
                case 3:
                    Console.Write("1 - по имени\n2 - по ID\nВвод: ");
                    input = Console.ReadLine();
                    while (input != "1" && input != "2")
                    {
                        Console.Write("Не верно! Еще раз: ");
                        input = Console.ReadLine();
                    }
                    if (input == "1")
                    {
                        Console.Write("Введите название файла: ");
                        filename = Console.ReadLine();
                    }
                    else
                    {
                        Console.Write("Введите ID: ");
                        filename = Console.ReadLine();
                    }
                    request = $"DELETE {input} {filename}";
                    break;
                case 4:
                    Console.Write("Введите название файла: ");
                    filename = Console.ReadLine();
                    Console.Write("Введите под каким именем сохранить: ");
                    string nameonserver = Console.ReadLine();
                    if (nameonserver == " " || nameonserver == "") nameonserver = "untitled";
                    if (File.Exists(directoryPath + filename))
                    {
                        try
                        {
                            using (FileStream fileStream = new FileStream(directoryPath + filename, FileMode.Open, FileAccess.Read))
                            {
                                using (MemoryStream memoryStream = new MemoryStream())
                                {
                                    fileStream.CopyTo(memoryStream);

                                    byte[] fileBytes = memoryStream.ToArray();
                                    request = $"SAVE {filename} {nameonserver} {Convert.ToBase64String(fileBytes)}";
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error occurred while reading file: {ex.Message}");
                        }
                    } else
                    {
                        Console.WriteLine("Такого файла нет");
                    }
                    break;
            }

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync("127.0.0.1", 8888);

                using (StreamWriter writer = new StreamWriter(new NetworkStream(socket)))
                {
                    await writer.WriteLineAsync(request);
                    Console.WriteLine("The request was sent.");
                    if (int.Parse(choose) == 5) break;
                    writer.Close();
                }

                using (StreamReader reader = new StreamReader(new NetworkStream(socket)))
                {
                    string response = await reader.ReadLineAsync();
                    switch (int.Parse(choose))
                    {
                        case 1:
                            if (response.Split()[0] == "200") Console.WriteLine($"The response says that the file was created! ID = {response.Split()[1]}\n");
                            else Console.WriteLine("The response says that creating the file was forbidden!\n");
                            break;
                        case 2:
                            if (response.Split()[0] == "200")
                            {
                                Console.WriteLine($"The file is Download. Name: {response.Split()[1]}\n");
                                SaveFile(response.Split()[2], response.Split()[1]);
                            }
                            else Console.WriteLine("The response says that the file was not found!\n");
                            break;
                        case 3:
                            if (response == "200") Console.WriteLine("The response says that the file was successfully deleted!\n");
                            else Console.WriteLine("The response says that the file was not found!\n");
                            break;
                        case 4:
                            if (response.Split()[0] == "200")
                            {
                                Console.WriteLine($"The response says that the file was saved! ID = {response.Split()[1]}\n");
                            }
                            else Console.WriteLine("The response says that creating the file was forbidden!\n");
                            break;
                        case 5:
                            if (response == "777") Console.WriteLine("The response says that the file was successfully deleted!\n");
                            else Console.WriteLine("The response says that the file was not found!\n");
                            break;
                    }
                    reader.Close();
                }
            }
            catch (SocketException)
            {
                Console.WriteLine($"Не удалось установить подключение с {socket.RemoteEndPoint}\n");
            }
        }
    }

    static void SaveFile(string base64String, string filename)
    {
        try
        {
            byte[] fileBytes = Convert.FromBase64String(base64String);
            string filePath = Path.Combine("./client/data/", filename);
            File.WriteAllBytes(filePath, fileBytes);
            Console.WriteLine($"Файл успешно сохранен: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении файла: {ex.Message}");
        }
    }
}