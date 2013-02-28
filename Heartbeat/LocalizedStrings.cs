using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Heartbeat
{
    public class LocalizedStrings
    {
        public LocalizedStrings()
        {
        }

        private static Heartbeat.AppResources localizedResources = new Heartbeat.AppResources();

        public Heartbeat.AppResources LocalizedResources { get { return localizedResources; } }
    }
}
