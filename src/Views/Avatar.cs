﻿using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace SourceGit.Views
{
    public class Avatar : Control, Models.IAvatarHost
    {
        private static readonly GradientStops[] FALLBACK_GRADIENTS = [
            new GradientStops() { new GradientStop(Colors.Orange, 0), new GradientStop(Color.FromRgb(255, 213, 134), 1) },
            new GradientStops() { new GradientStop(Colors.DodgerBlue, 0), new GradientStop(Colors.LightSkyBlue, 1) },
            new GradientStops() { new GradientStop(Colors.LimeGreen, 0), new GradientStop(Color.FromRgb(124, 241, 124), 1) },
            new GradientStops() { new GradientStop(Colors.Orchid, 0), new GradientStop(Color.FromRgb(248, 161, 245), 1) },
            new GradientStops() { new GradientStop(Colors.Tomato, 0), new GradientStop(Color.FromRgb(252, 165, 150), 1) },
        ];

        public static readonly StyledProperty<Models.User> UserProperty =
            AvaloniaProperty.Register<Avatar, Models.User>(nameof(User));

        public Models.User User
        {
            get => GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        static Avatar()
        {
            UserProperty.Changed.AddClassHandler<Avatar>(OnUserPropertyChanged);
        }

        public Avatar()
        {
            var refetch = new MenuItem() { Header = App.Text("RefetchAvatar") };
            refetch.Click += (_, _) =>
            {
                if (User != null)
                    Models.AvatarManager.Instance.Request(User.Email, true);
            };

            ContextMenu = new ContextMenu();
            ContextMenu.Items.Add(refetch);

            RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
        }

        public override void Render(DrawingContext context)
        {
            if (User == null)
                return;

            var corner = (float)Math.Max(2, Bounds.Width / 16);
            var img = Models.AvatarManager.Instance.Request(User.Email, false);
            if (img != null)
            {
                var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
                context.PushClip(new RoundedRect(rect, corner));
                context.DrawImage(img, rect);
            }
            else
            {
                Point textOrigin = new Point((Bounds.Width - _fallbackLabel.Width) * 0.5, (Bounds.Height - _fallbackLabel.Height) * 0.5);
                context.DrawRectangle(_fallbackBrush, null, new Rect(0, 0, Bounds.Width, Bounds.Height), corner, corner);
                context.DrawText(_fallbackLabel, textOrigin);
            }
        }

        public void OnAvatarResourceChanged(string email)
        {
            if (User.Email.Equals(email, StringComparison.Ordinal))
                InvalidateVisual();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            Models.AvatarManager.Instance.Subscribe(this);
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            Models.AvatarManager.Instance.Unsubscribe(this);
        }

        private static void OnUserPropertyChanged(Avatar avatar, AvaloniaPropertyChangedEventArgs e)
        {
            if (avatar.User == null)
                return;

            var fallback = GetFallbackString(avatar.User.Name);
            var chars = fallback.ToCharArray();
            var sum = 0;
            foreach (var c in chars)
                sum += Math.Abs(c);

            avatar._fallbackBrush = new LinearGradientBrush
            {
                GradientStops = FALLBACK_GRADIENTS[sum % FALLBACK_GRADIENTS.Length],
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            };

            var typeface = new Typeface("fonts:SourceGit#JetBrains Mono");
            avatar._fallbackLabel = new FormattedText(
                fallback,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                avatar.Width * 0.65,
                Brushes.White);

            avatar.InvalidateVisual();
        }

        private static string GetFallbackString(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var chars = new List<char>();
            foreach (var part in parts)
                chars.Add(part[0]);

            if (chars.Count >= 2 && char.IsAsciiLetterOrDigit(chars[0]) && char.IsAsciiLetterOrDigit(chars[^1]))
                return string.Format("{0}{1}", chars[0], chars[^1]);

            return name.Substring(0, 1);
        }

        private FormattedText _fallbackLabel = null;
        private LinearGradientBrush _fallbackBrush = null;
    }
}
