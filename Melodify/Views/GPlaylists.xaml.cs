﻿using SpotifyAPI;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Melodify
{
    /// <summary>
    /// Interaction logic for GPlaylists.xaml
    /// </summary>
    public partial class GPlaylists : Window
    {
        SpotifyWebAPI _spotify = new SpotifyWebAPI()

        {
            AccessToken = (string)App.Current.Properties["AccessToken"],
            TokenType = (string)App.Current.Properties["TokenType"]
        };

        public GPlaylists()
        {
            InitializeComponent();
            WindowBlur.SetIsEnabled(this, true);
            MouseDown += Window_MouseDown;
            Populate_Playlists();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Populate_Playlists()
        {
            Paging<SimplePlaylist> userPlaylists = _spotify.GetUserPlaylists(_spotify.GetPrivateProfile().Id);
            foreach (SimplePlaylist playlist in userPlaylists.Items)
            {
                Grid grid = new Grid();

                // Get the image URL
                BitmapImage bimage = new BitmapImage();
                bimage.BeginInit();
                bimage.UriSource = new Uri(playlist.Images[0].Url, UriKind.Absolute);
                bimage.EndInit();

                // Bind the image to a brush
                ImageBrush imageBrush = new ImageBrush();
                imageBrush.ImageSource = bimage;
                imageBrush.Stretch = System.Windows.Media.Stretch.UniformToFill;

                // Create the image container
                Ellipse ellipse = new Ellipse();
                ellipse.Height = 100;
                ellipse.Width = 100;
                ellipse.Fill = imageBrush;
                ellipse.Margin = new Thickness(0, 10, 0, 0);
                ellipse.VerticalAlignment = System.Windows.VerticalAlignment.Top;

                // Create the song text
                TextBlock tBlock = new TextBlock();

                string pName = playlist.Name;
                tBlock.Text = pName;

                tBlock.FontSize = 16;
                tBlock.Margin = new Thickness(0, 0, 0, 35);
                tBlock.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                tBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                tBlock.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFC8C8C8");

                grid.Cursor = Cursors.Hand;
                grid.MouseDown += ((s, e) => Play_Playlist(s, e, playlist.Uri));

                grid.Children.Add(ellipse);
                grid.Children.Add(tBlock);

                playlists.Children.Add(grid);
            }
        }

        private void Play_Playlist(object sender, MouseEventArgs e, string playlistURI)
        {
            // Save their current playback so we can return to it after
            try
            {
                PlaybackContext context = _spotify.GetPlayingTrack();
                PlaybackContext playbackContext = _spotify.GetPlayback();
                if (context.Context.Type == "playlist")
                {
                    string playlistID = playbackContext.Context.Uri;
                    App.Current.Properties["playlistID"] = playlistID;
                    App.Current.Properties["suggestionMode"] = true;
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Issue saving playback at TopSongs/Preview_Song");
            }

            ErrorResponse err = _spotify.ResumePlayback(contextUri: playlistURI, offset: "");
            // Possibility of disabling repeat when playing back
            //App.Current.Properties["setRepeatOff"] = true;
            //ErrorResponse error = _spotify.SetRepeatMode(RepeatState.Off) ;
        }

    }
}
