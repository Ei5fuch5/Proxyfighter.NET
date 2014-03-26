using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using WpfProxyTool.Model;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Data;
using System.IO;
using System.Net;

namespace WpfProxyTool.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        public ObservableCollection<ProxyLeechListModel> leechList { get; set; }
        public LeecherModel leechModel { get; set; }
        private static object listLock = new object();

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            leechList = new ObservableCollection<ProxyLeechListModel>();
            BindingOperations.EnableCollectionSynchronization(leechList, listLock);
            leechModel = new LeecherModel();
            leechModel.LeechTimeout = "5";
            leechModel.LeechStartButtonEnabled = true;
            leechModel.ProgressBarEnabled = false;

            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
        }

        private RelayCommand openLeechListCommand;
        public RelayCommand OpenLeechListCommand
        {
            get
            {
                {
                    if (openLeechListCommand == null)
                        openLeechListCommand = new RelayCommand(new Action(openLeechListExecuted));
                    return openLeechListCommand;
                }
            }
        }

        private void openLeechListExecuted()
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.InitialDirectory = @"C:\";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Task.Factory.StartNew( () => readLeechFile(ofd.FileName));
                //MessageBox.Show(ofd.FileName);
            }
        }

        private void readLeechFile(String path)
        {
            string file = System.IO.File.ReadAllText(path);
            // http://regexlib.com/Search.aspx?k=url&AspxAutoDetectCookieSupport=1
            Regex regex = new Regex(@"(http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?");
            MatchCollection matches = regex.Matches(file);
            foreach (Match match in matches)
            {
                leechList.Add(new ProxyLeechListModel
                    {
                        URL = match.ToString()
                    }
                );
            }
        }

        private RelayCommand<DragEventArgs> dataGridLeecherDropCommand;
        public RelayCommand<DragEventArgs> DataGridLeecherDropCommand
        {
            get
            {
                return dataGridLeecherDropCommand ?? (dataGridLeecherDropCommand = new RelayCommand<DragEventArgs>(dataGridLeecherDrop));
            }
        }
        private void dataGridLeecherDrop(DragEventArgs e)
        {
            // http://stackoverflow.com/questions/11671803/get-source-of-a-dragdrop
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Task.Factory.StartNew(() => readLeechFile(files[0]));
        }


        private RelayCommand startLeechingCommand;
        public RelayCommand StartLeechingCommand
        {
            get
            {
                {
                    if (startLeechingCommand == null)
                        startLeechingCommand = new RelayCommand(new Action(startLeechingExecuted));
                    return startLeechingCommand;
                }
            }
        }

        private void startLeechingExecuted()
        {
            leechModel.LeechStartButtonEnabled = false;
            leechModel.ProgressBarEnabled = true;

            taskForEach(5);
            //parallelForEach();

            //leechModel.ProgressBarEnabled = false;
            //leechStartButtonEnabled = true;
        }

        private void parallelForEach()
        {
            // Solution? http://stackoverflow.com/questions/12337671/using-async-await-for-multiple-tasks
            Parallel.ForEach(leechList, async item =>
                {
                    WebContent source = new WebContent();
                    source = await getURLContentAsync(item.URL);

                    string content = System.Text.Encoding.Default.GetString(source.content);
                    item.Proxys.Add(content);
                    item.Date = DateTime.Now;
                    item.Count = source.content.Length;
                    item.Reply = source.status;
                }
            );
        }

        private void taskForEach(int taskCount)
        {
            // http://msdn.microsoft.com/de-de/library/dd270695%28v=vs.110%29.aspx
            // Problem mit Async
            //for (int i = 0; i < leechList.Count; i+=taskCount - 1)
            for (int i = 0; i < 4; i += 5)
            {
                List<ProxyLeechListModel> items = new List<ProxyLeechListModel>();
                for (int t = i; t < i + taskCount; t++)
                {
                    items.Add(leechList[t]);
                }

                Task[] tasks = new Task[4];
                for (int v = 0; v < taskCount - 1; v++)
                {
                    if (v == 5 ) 
                        MessageBox.Show("Ist 5!!!!");
                    if (v < items.Count)
                    {
                        tasks[v] = new Task(() => makeTaskRequest(leechList.IndexOf(items[v])));
                    }
                }
                Task.WaitAll(tasks);
            }
        }

        private async void makeTaskRequest(int index)
        {
            WebContent source = new WebContent();
            source = await getURLContentAsync(leechList[index].URL);

            string content = System.Text.Encoding.Default.GetString(source.content);
            leechList[index].Proxys.Add(content);
            leechList[index].Date = DateTime.Now;
            leechList[index].Count = source.content.Length;
            leechList[index].Reply = source.status;
        }

        private async Task<WebContent> getURLContentAsync(string url)
        {
            // http://msdn.microsoft.com/de-de/library/hh300224.aspx
            WebContent result = new WebContent();
            try
            {
                var content = new MemoryStream();
                var webReq = (HttpWebRequest)WebRequest.Create(url);
                webReq.Timeout = Int32.Parse(leechModel.LeechTimeout) * 1000;
                using (WebResponse response = await webReq.GetResponseAsync())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        await responseStream.CopyToAsync(content);
                        result.content = content.ToArray();
                        result.status = ((HttpWebResponse)response).StatusCode.ToString();
                    }
                }
            }
            catch (WebException ex)
            {
                result.content = new byte[0];
                if (ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    result.status = resp.StatusCode.ToString();
                }
                else
                {
                    result.status = "Not Found";
                }
            }
            return result;
        }

        private RelayCommand clearLeechListCommand;
        public RelayCommand ClearLeechListCommand
        {
            get
            {
                {
                    if (clearLeechListCommand == null)
                        clearLeechListCommand = new RelayCommand(new Action(ClearLeechListExecuted));
                    return clearLeechListCommand;
                }
            }
        }

        private void ClearLeechListExecuted()
        {
            leechList.Clear();
        }
    }
}