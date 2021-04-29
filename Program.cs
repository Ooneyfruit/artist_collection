using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Net;

namespace artist_collector //Environment.Exit(0);
{
    struct Song
    {
        public string title;
        public string artist;
    }

    class User
    {
        public int total_pages;

        public string username;
        public string key;
        public string main_directory;
        public string file_destination;
        public string remote_uri;
        
        public User(string username, string key)
        {
            this.username = username;
            this.key = key;
            total_pages = 1;
            main_directory = Directory.GetCurrentDirectory() + "/";
            file_destination = main_directory + username + "/";
            remote_uri = $"http://ws.audioscrobbler.com/2.0/?method=user.getTopTracks&period=overall&limit=1000&api_key={key}&username={username}&page=";
        
            DirectoryInfo directory = new DirectoryInfo(file_destination);
            if (directory.Exists == false)
                directory.Create();
        }

        public void DownloadTopTracks()
        {
            WebClient web_client = new WebClient();

            string web_resource;
            string file_name;
            bool failure;

            int page = 1;
            while (page <= total_pages) 
            {
                web_resource = remote_uri + page.ToString();
                file_name = file_destination + page.ToString() + ".xml";

                failure = true;
                while(failure)
                {
                    try
                    {
                        web_client.DownloadFile(web_resource, file_name);
                        failure = false;
                    }
                    catch
                    {
                        Console.WriteLine("Attempt failed, trying again (ctrl+c to quit)");
                    }
                }

                if (total_pages == 1)
                {
                    XmlReader reader = XmlReader.Create(file_name);
                    reader.ReadToFollowing("toptracks");
                    total_pages = int.Parse(reader.GetAttribute("totalPages"));
                    Console.WriteLine($"There are {total_pages} total pages");
                }

                Console.WriteLine($"Downloaded: {web_resource}\nTo: {file_name}");
                page += 1;
            }
        }
        
        public void FindNewTracks()
        {
            string file_songs = file_destination + username + ".txt";
            string file_artists = main_directory + "artists.txt";

            if (!File.Exists(file_artists))
            {
                StreamWriter sw = File.CreateText(file_artists);
            }

            string[] lines_artists = File.ReadAllLines(file_artists);
            HashSet<string> found_artists = new HashSet<string>(lines_artists);
            List<string> found_songs = new List<string>();

            bool found = true;
            string file_name;

            for (int page = 1; page <= total_pages; page++)
            {
                file_name = file_destination + page.ToString() + ".xml";
                XmlReader reader = XmlReader.Create(file_name);
                Song song = new Song();
                do 
                {
                    if (reader.MoveToContent() == XmlNodeType.Element && reader.Name == "name")
                    {
                        if (found)
                        {
                            song.title = reader.ReadElementContentAsString();
                            found = false;
                            continue;
                        }
                        song.artist = reader.ReadElementContentAsString();
                        
                        found = true;
                        if (!found_artists.Contains(song.artist))
                        {
                            found_artists.Add(song.artist);
                            found_songs.Add($"{song.artist} - {song.title}");
                        }
                    } 
                } while (reader.Read());
                Console.WriteLine($"Read page {page}");
            }
            File.WriteAllLines(file_artists, found_artists);
            File.WriteAllLines(file_songs, found_songs);
            Console.WriteLine($"Updated: {file_artists}\nSaved new songs to: {file_songs}.txt");
        }
    }

    class Application
    {
        static void Main(string[] args)
        {
            User user = new User("Ooneyfruit","1762d9812e91c8a8ef1f45ecf4eeecf3");
            user.DownloadTopTracks();
            user.FindNewTracks();
        }
    }
}
