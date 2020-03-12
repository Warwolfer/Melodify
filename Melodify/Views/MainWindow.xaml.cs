﻿using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;
using System;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Melodify
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string _songID;
        int progress = 0;
        SpotifyAPI spotAPI;
        SpotifyWebAPI _spotify;
        bool _pauseAPI = false;

        public MainWindow()
        {
            InitializeComponent();

            // Sets the position of the window according to history
            if (Properties.Settings.Default.firstLaunch)
            {
                Left = SystemParameters.WorkArea.Width - Width;
                Top = SystemParameters.WorkArea.Height - Height;

                Properties.Settings.Default.mainLeft = Left;
                Properties.Settings.Default.mainTop = Top;

                Properties.Settings.Default.firstLaunch = false;
            } else
            {
                Left = Properties.Settings.Default.mainLeft;
                Top = Properties.Settings.Default.mainTop;
            }

            // Initalize some instances to ensure continuing playback
            App.Current.Properties["userPause"] = false;
            spotAPI = new SpotifyAPI(Properties.Resources.SpotID, Properties.Resources.SpotSecret);

            // Sleep for a second while waiting for login to process
            Thread.Sleep(1000);
            _spotify = new SpotifyWebAPI()
            {
                AccessToken = (string)Application.Current.Properties["AccessToken"],
                TokenType = (string)Application.Current.Properties["TokenType"]
            };

            // Timer to get the information
            System.Timers.Timer timer = new System.Timers.Timer(350);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            // Timer to refresh the access token
            System.Timers.Timer accesser = new System.Timers.Timer(3500000);
            accesser.Elapsed += Access_Elapsed;
            accesser.Start();

            // Checks user settings regarding progress bar
            if (Properties.Settings.Default.ProgressBar)
            {
                Progressbar.Header = "Enable Progress Bar";
                progressGrid.Visibility = Visibility.Collapsed;
            } else
            {
                Progressbar.Header = "Disable Progress Bar";
                progressGrid.Visibility = Visibility.Visible;
            }

            // User now ran the software at least once
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // If we're looking to make the timer temporarily useless to reduce API rate call
            if (_pauseAPI) { return; }

            try
            {
                PlaybackContext context = _spotify.GetPlayingTrack();
                // Take control over the UI elements
                Dispatcher.Invoke(() =>
                {
                    if (context.Error == null && context.Item != null)
                    {
                        if (context.Item.Error == null)
                        {
                            Title.Content = context.Item.Name;
                            Author.Content = context.Item.Artists[0].Name;

                            // Check to make sure the song changed before refreshing variables
                            if ((context.Item.Album.Images[0].Url != cover.Source.ToString()) && (context.Item.Album.Images[0].Url != null)) {
                                BitmapImage albumArt = new BitmapImage();
                                albumArt.BeginInit();
                                albumArt.UriSource = new Uri(context.Item.Album.Images[0].Url);
                                albumArt.EndInit();
                                cover.Source = albumArt;

                                // Sets to solid heart if we liked it already
                                _songID = context.Item.Id;
                                ListResponse<bool> tracksSaved = _spotify.CheckSavedTracks(new System.Collections.Generic.List<String> { _songID });
                                if (tracksSaved.List[0]) {
                                    loveClick.Content = "♥";
                                } else {
                                    loveClick.Content = "♡";
                                }

                                // Gets the total length of the song
                                progress = context.Item.DurationMs;
                            }

                            // Animates the progress bar to fluidly move to position
                            DoubleAnimation animation = new DoubleAnimation();
                            animation.From = progressBar.ActualWidth;
                            animation.To = ((double)(context.ProgressMs) / (double)(progress)) * this.Width;
                            animation.Duration = new Duration(TimeSpan.FromMilliseconds(250));
                            animation.FillBehavior = FillBehavior.Stop;
                            progressBar.BeginAnimation(Rectangle.WidthProperty, animation);
                            progressBar.Width = ((double)(context.ProgressMs) / (double)(progress)) * this.Width;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Inside- " + context.Error.Message);
                        }
                    }
                    else if (context.Error != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Outside- " + context.Error.Message);

                        // If the issue is that the auth didn't go through, we try to set up a new connection
                        if (context.Error.Message == "Only valid bearer authentication supported")
                        {
                            _spotify = new SpotifyWebAPI()
                            {
                                AccessToken = (string)Application.Current.Properties["AccessToken"],
                                TokenType = (string)Application.Current.Properties["TokenType"]
                            };
                        } else if (context.Error.Message == "The access token expired")
                        {
                            Debug.WriteLine("Attempting to refresh token due to expiration");
                            spotAPI.Authenticate();
                            _spotify = new SpotifyWebAPI()
                            {
                                AccessToken = (string)Application.Current.Properties["AccessToken"],
                                TokenType = (string)Application.Current.Properties["TokenType"]
                            };
                        }
                    }
                });
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine("Error main timer- " + err.Message);
            }
        }

        // Function to reauthenticate the API
        private void Access_Elapsed(object sender, ElapsedEventArgs e)
        {
            spotAPI.Authenticate();
            _spotify = new SpotifyWebAPI()
            {
                AccessToken = (string)Application.Current.Properties["AccessToken"],
                TokenType = (string)Application.Current.Properties["TokenType"]
            };
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Full_Click(object sender, RoutedEventArgs e)
        {
            FullNow(true);
            FullScreen fullScreen = new FullScreen(this);
            fullScreen.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            fullScreen.Show();
            fullScreen.WindowState = WindowState.Maximized;
        }

        private new void Mouse_Wheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                Spotify.GetSetVolume(Spotify.GetSetVolume() + 10);
            }

            else if (e.Delta < 0)
            {
                Spotify.GetSetVolume(Spotify.GetSetVolume() - 10);
            }
        }

        public void FullNow(bool pauseAPI)
        {
            _pauseAPI = pauseAPI;
        }

        private void Love_Click(object sender, RoutedEventArgs e)
        {
            ListResponse<bool> tracksSaved = _spotify.CheckSavedTracks(new System.Collections.Generic.List<String> { _songID });
            if (tracksSaved.List[0])
            {
                Spotify.UnLoveSong(_songID);
                loveClick.Content = "♡";
            } else
            {
                Spotify.LoveSong(_songID);
                loveClick.Content = "♥";
            }
        }

        private void Menu_Click(object sender, RoutedEventArgs e)
        {
            MainMenu mainMenu = new MainMenu();
            mainMenu.Show();
        }

        private void PausePlay_Click(object sender, RoutedEventArgs e)
        {
            Spotify.PausePlaySong();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            Spotify.NextSong();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            Spotify.PreviousSong();
        }

        private void Seek_Playback(object sender, RoutedEventArgs e)
        {
            int position = (int)((Mouse.GetPosition(this).X / 360) * 100);
            Spotify.SeekPlayback(position);
            e.Handled = true;
        }

        private void Playlist_Click(object sender, RoutedEventArgs e)
        {
            PlaylistMenu playlistMenu = new PlaylistMenu();
            playlistMenu.Show();
        }

        private void MainGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            fullClick.Visibility = Visibility.Collapsed;
            loveClick.Visibility = Visibility.Collapsed;
            infoClick.Visibility = Visibility.Collapsed;
            playlistClick.Visibility = Visibility.Collapsed;
        }

        private void MainGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            fullClick.Visibility = Visibility.Visible;
            loveClick.Visibility = Visibility.Visible;
            infoClick.Visibility = Visibility.Visible;
            playlistClick.Visibility = Visibility.Visible;
        }

        private void CMExit_Click(object sender, RoutedEventArgs e)
        {
            if (TaskContext.Header.ToString() == "Main View")
            {
                Properties.Settings.Default.taskTop = Top;
                Properties.Settings.Default.taskLeft = Left;
            }
            else
            {
                Properties.Settings.Default.mainTop = Top;
                Properties.Settings.Default.mainLeft = Left;
            }

            Properties.Settings.Default.Save();
            Application.Current.Shutdown();
        }

        private void CMGithub_Click(object sender, RoutedEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd",
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"/c start https://github.com/novatorem/Melodify"
            };
            Process.Start(psi);
        }

        private void CMInfo_Click(object sender, RoutedEventArgs e)
        {
            AppInfo appInfo = new AppInfo();
            appInfo.Show();
        }

        private void Progressbar_Click(object sender, RoutedEventArgs e)
        {
            if (progressGrid.Visibility == Visibility.Visible)
            {
                Progressbar.Header = "Enable Progress Bar";
                progressGrid.Visibility = Visibility.Collapsed;
                Properties.Settings.Default.ProgressBar = false;
            }
            else if (progressGrid.Visibility == Visibility.Collapsed)
            {
                Progressbar.Header = "Disable Progress Bar";
                progressGrid.Visibility = Visibility.Visible;
                Properties.Settings.Default.ProgressBar = true;
            }
            Properties.Settings.Default.Save();
        }

        private void Populate_Playlists(object sender, RoutedEventArgs e)
        {
            Playlists playlists = new Playlists(true);
            playlists.Show();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                Spotify.PreviousSong();
            } else if (e.Key == Key.Right)
            {
                Spotify.NextSong();
            } else if (e.Key == Key.Space ^ e.Key == Key.Enter)
            {
                Spotify.PausePlaySong();
            } else if (e.Key == Key.F)
            {
                FullNow(true);
                FullScreen fullScreen = new FullScreen(this);
                fullScreen.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                fullScreen.Show();
                fullScreen.WindowState = WindowState.Maximized;
            }
            e.Handled = true;
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FullNow(true);
            FullScreen fullScreen = new FullScreen(this);
            fullScreen.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            fullScreen.Show();
            fullScreen.WindowState = WindowState.Maximized;
            e.Handled = true;
        }

        private void progressGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            progressGrid.Height = 8;
            progressBar.Height = 3;
        }

        private void progressGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            progressGrid.Height = 6;
            progressBar.Height = 2;
        }

        private void TaskContext_Click(object sender, RoutedEventArgs e)
        {
            if (TaskContext.Header.ToString() == "Task View")
            {
                Title.Margin = new Thickness();
                Author.Visibility = Visibility.Hidden;

                // Save the values we've gotten for Main View
                Properties.Settings.Default.mainTop = Top;
                Properties.Settings.Default.mainLeft = Left;

                if (Properties.Settings.Default.firstTaskView)
                {
                    Top = SystemParameters.WorkArea.Height;
                    Left = SystemParameters.WorkArea.Width - Width - (Width / 2);
                    Properties.Settings.Default.firstTaskView = false;
                }
                else
                {
                    Top = Properties.Settings.Default.taskTop;
                    Left = Properties.Settings.Default.taskLeft;
                }

                Height = SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height;

                fullClick.Margin = new Thickness(-1, -1, -1, -1);
                loveClick.Margin = new Thickness(-1, -1, -1, -1);
                infoClick.Margin = new Thickness(-1, -1, -1, -1);
                playlistClick.Margin = new Thickness(-1, -1, -1, -1);

                pauseButton.Padding = new Thickness(50, 10, 50, 0);
                previousButton.Padding = new Thickness(50, 10, 50, 0);
                nextButton.Padding = new Thickness(50, 10, 50, 0);

                TaskContext.Header = "Main View";
            }
            else
            {
                Height = 100;
                Author.Visibility = Visibility.Visible;
                Title.Margin = new Thickness(0, 0, 0, 20);

                // Save the values we've gotten for Task View

                Properties.Settings.Default.taskTop = Top;
                Properties.Settings.Default.taskLeft = Left;

                Top = Properties.Settings.Default.mainTop;
                Left = Properties.Settings.Default.mainLeft;

                fullClick.Margin = new Thickness(5, 5, 0, 0);
                loveClick.Margin = new Thickness(0, 0, 5, 5);
                infoClick.Margin = new Thickness(5, 5, 5, 0);
                playlistClick.Margin = new Thickness(5, 0, 0, 5);

                pauseButton.Padding = new Thickness(50, 40, 50, 25);
                previousButton.Padding = new Thickness(50, 20, 50, 25);
                nextButton.Padding = new Thickness(50, 20, 50, 25);

                TaskContext.Header = "Task View";
            }
        }

        private void ResetPosition_Click(object sender, RoutedEventArgs e)
        {
            if (TaskContext.Header.ToString() == "Main View")
            {
                Top = SystemParameters.WorkArea.Height;
                Left = SystemParameters.WorkArea.Width - Width - (Width / 2);

                Properties.Settings.Default.taskTop = Top;
                Properties.Settings.Default.taskLeft = Left;
            }
            else
            {
                Left = SystemParameters.WorkArea.Width - Width;
                Top = SystemParameters.WorkArea.Height - Height;

                Properties.Settings.Default.mainTop = Top;
                Properties.Settings.Default.mainLeft = Left;
            }
        }
    }
}
