﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfProxyTool.Model
{
    public class ProxyLeechListModel : System.ComponentModel.INotifyPropertyChanged
    {
        public ProxyLeechListModel()
        {

        }

        // Need to notify the WPF elements if any of the properties changed on a Person object
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }

        private string _url;
        public string URL
        {
            get
            {
                return _url;
            }
            set
            {
                _url = value;
                OnPropertyChanged("URL");
            }
        }


        private string reply;
        public string Reply
        {
            get
            {
                return reply;
            }
            set
            {
                reply = value;
                OnPropertyChanged("Reply");
            }
        }


        private int count;
        public int Count
        {
            get
            {
                return count;
            }
            set
            {
                count = value;
                OnPropertyChanged("Count");
            }
        }


        private DateTime date;
        public DateTime Date
        {
            get
            {
                return date;
            }
            set
            {
                date = value;
                OnPropertyChanged("Date");
            }
        }

        public bool IsSelected { get; set; }
    }
}
