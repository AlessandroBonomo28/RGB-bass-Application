using Android.App;
using Android.Bluetooth;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using AndroidX.AppCompat.App;
using Java.Util;
using System;
using System.IO;
using System.Linq;

namespace commandSenderHC05
{
    [Activity(Label = "@string/app_name",Icon ="@mipmap/ic_launcher", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : Activity
    {

        private BluetoothAdapter mBluetoothAdapter = null;
        private BluetoothSocket btSocket = null;

        private Stream outStream = null;
        //MAC Address del dispositivo Bluetooth
        private static string address = "98:D3:31:F9:54:34";
        //Id Unico di comunicazione
        private static UUID MY_UUID = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");
        TextView connStateTextView;
        TextView debugTextView;

        Button btnSetColor;
        Button btnSetAnimColor;
        Button btnPlayAnim;

        EditText editTextRedCode, editTextGreenCode, editTextBlueCode;
        Spinner spinnerPickAnim;
        Spinner spinnerPickMicAnim;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activity_main);
            // SPINNER

            Spinner spinnerPickColor = FindViewById<Spinner>(Resource.Id.spinnerPickColor);

            spinnerPickColor.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinnerPickColor_ItemSelected);
            var adapterPickColor = ArrayAdapter.CreateFromResource(
                    this, Resource.Array.color_array, Android.Resource.Layout.SimpleSpinnerItem);

            adapterPickColor.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinnerPickColor.Adapter = adapterPickColor;

            spinnerPickAnim = FindViewById<Spinner>(Resource.Id.spinnerPickAnim);

            spinnerPickAnim.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinnerAnimColor_ItemSelected);
            var adapterPickAnim = ArrayAdapter.CreateFromResource(
                    this, Resource.Array.anim_array, Android.Resource.Layout.SimpleSpinnerItem);

            adapterPickAnim.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinnerPickAnim.Adapter = adapterPickAnim;

            spinnerPickMicAnim = FindViewById<Spinner>(Resource.Id.spinnerPickAnimMic);

            spinnerPickMicAnim.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinnerMicAnim_ItemSelected);
            var adapterMicPickAnim = ArrayAdapter.CreateFromResource(
                    this, Resource.Array.micAnim_array, Android.Resource.Layout.SimpleSpinnerItem);

            adapterMicPickAnim.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinnerPickMicAnim.Adapter = adapterMicPickAnim;

            // BUTTON
            Button btnConnect = FindViewById<Button>(Resource.Id.buttonConnect);
            Button btnDisconnect = FindViewById<Button>(Resource.Id.buttonDisonnect);
            Button btnHexToRGB = FindViewById<Button>(Resource.Id.buttonHexValue);
            btnSetColor = FindViewById<Button>(Resource.Id.buttonSetColor);
            btnSetAnimColor = FindViewById<Button>(Resource.Id.buttonSetAnimColor);
            btnPlayAnim = FindViewById<Button>(Resource.Id.buttonPlayAnimation);
            Button btnSwapColors = FindViewById<Button>(Resource.Id.buttonSwap);
            Button btnNoBlend = FindViewById<Button>(Resource.Id.buttonNoBlend);
            Button btnBlend = FindViewById<Button>(Resource.Id.buttonBlend);
            Button btnMicModeOn = FindViewById<Button>(Resource.Id.buttonMicModeOn);
            Button btnMicModeOff = FindViewById<Button>(Resource.Id.buttonMicModeOff);
            Button btnSetMicAnim = FindViewById<Button>(Resource.Id.buttonSetMicAnimation);

            // EDIT TEXT
            EditText editTextHex = FindViewById<EditText>(Resource.Id.edittextHex);

            editTextRedCode = FindViewById<EditText>(Resource.Id.edittextRed);
            editTextGreenCode = FindViewById<EditText>(Resource.Id.edittextGreen);
            editTextBlueCode = FindViewById<EditText>(Resource.Id.edittextBlue);
            // TEXT VIEW
            connStateTextView = FindViewById<TextView>(Resource.Id.textViewStato);
            debugTextView = FindViewById<TextView>(Resource.Id.textViewDebug);
            //SEEK BAR
            SeekBar seekbarBri = FindViewById<SeekBar>(Resource.Id.seekbarBrightness);

            VerificaBluetoothAttivo();
            // BUTTON ACTIONS
            btnHexToRGB.Click += delegate
            {
                Color inputColor = hexToRGB(editTextHex.Text);
                setRGBeditTexts(inputColor);
            };

            btnConnect.Click += delegate {
                // tenta connessione
                if (btSocket != null)
                    if (btSocket.IsConnected)
                    {
                        debugTextView.Text = "gia' connesso";
                        return;
                    }

                ConnettiAdArduino();
            };

            btnDisconnect.Click += delegate {
                DisconnettiDaArduino();
                // disconnetti
            };

            btnSetColor.Click += delegate
            {
                if(!isValidRGBedit())
                {
                    Toast.MakeText(this, "Cannot set: invalid color", ToastLength.Short).Show();
                    return;
                }
                string msg = "!setrgb{" +
                    editTextRedCode.Text.ToString() + ";" + editTextGreenCode.Text.ToString() + ";" + editTextBlueCode.Text.ToString()+"}-";
                writeData(new Java.Lang.String(msg));
                string toast = string.Format("Trying to set {0} as color", getColorStringFromRGBedit());
                Toast.MakeText(this, toast, ToastLength.Short).Show();
            };

            btnSetAnimColor.Click += delegate
            {
                if (!isValidRGBedit())
                {
                    Toast.MakeText(this, "Cannot set: invalid color", ToastLength.Short).Show();
                    return;
                }
                string msg = "!setargb{" +
                    editTextRedCode.Text.ToString() + ";" + editTextGreenCode.Text.ToString() + ";" + editTextBlueCode.Text.ToString() + "}-";
                writeData(new Java.Lang.String(msg));
                string toast = string.Format("Trying to set {0} as anim color", getColorStringFromRGBedit());
                Toast.MakeText(this, toast, ToastLength.Short).Show();
            };

            btnPlayAnim.Click += delegate
            {
                Spinner spinner = spinnerPickAnim;
                string selected = spinner.GetItemAtPosition(spinner.SelectedItemPosition).ToString();
                if (selected.Equals("None"))
                {
                    return;
                }

                string msg = "!playanim{" +selected+ "}-";
                writeData(new Java.Lang.String(msg));

                string toast = string.Format("Trying to play anim {0}",selected);
                Toast.MakeText(this, toast, ToastLength.Short).Show();
            };

            btnBlend.Click += delegate
            {
                string msg = "!setblend{Y}-";
                writeData(new Java.Lang.String(msg));
            };

            btnNoBlend.Click += delegate
            {
                string msg = "!setblend{N}-";
                writeData(new Java.Lang.String(msg));
            };

            btnMicModeOn.Click += delegate
            {
                string msg = "!micmode{Y}-";
                writeData(new Java.Lang.String(msg));
            };

            btnMicModeOff.Click += delegate
            {
                string msg = "!micmode{N}-";
                writeData(new Java.Lang.String(msg));
            };

            btnSetMicAnim.Click += delegate
            {
                Spinner spinner = spinnerPickMicAnim;
                string selected = spinner.GetItemAtPosition(spinner.SelectedItemPosition).ToString();
                if (selected.Equals("None"))
                {
                    return;
                }

                string msg = "!micanim{" + selected + "}-";
                writeData(new Java.Lang.String(msg));

                string toast = string.Format("Trying to set anim {0}", selected);
                Toast.MakeText(this, toast, ToastLength.Short).Show();
            };

            btnSwapColors.Click += delegate
            {
                string msg = "!swap{}-";
                writeData(new Java.Lang.String(msg));
            };

            // SEEK BAR ACTIONS
            seekbarBri.ProgressChanged += delegate
            {
                string msg = "!setbright{"+seekbarBri.Progress+"}-";
                writeData(new Java.Lang.String(msg));
            };
        }
        private string getColorStringFromRGBedit()
        {
            return editTextRedCode.Text +" "+
            editTextGreenCode.Text +" "+
            editTextBlueCode.Text ;
        }
        private bool isValidRGBedit()
        {
            try
            {
                int r = Int32.Parse(editTextRedCode.Text);
                int g = Int32.Parse(editTextGreenCode.Text);
                int b = Int32.Parse(editTextBlueCode.Text);
                if (r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255)
                    return true;
                else
                    return false;
            }
            catch(Exception e)
            {
                return false;
            }
            
        }
        private void setRGBeditTexts(Color c)
        {
            editTextRedCode.Text = c.R.ToString();
            editTextGreenCode.Text = c.G.ToString();
            editTextBlueCode.Text = c.B.ToString();
            
            btnSetColor.SetBackgroundColor(c);
            btnSetAnimColor.SetBackgroundColor(c);
            if(c.Equals(Color.White))
            {
                btnSetColor.SetTextColor(Color.Black);
                btnSetAnimColor.SetTextColor(Color.Black);
            }
            else
            {
                float[] hsv = new float[3];
                Color.ColorToHSV(c, hsv);
                hsv[0] = (hsv[0] + 180) % 360;
                c = Color.HSVToColor(hsv);
                btnSetColor.SetTextColor(c);
                btnSetAnimColor.SetTextColor(c);
            }
            
        }
        private void spinnerPickColor_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            if (spinner.GetItemAtPosition(e.Position).Equals("None"))
            {
                return;
            }
            Color inputColor = hexToRGB(spinner.GetItemAtPosition(e.Position).ToString());
            setRGBeditTexts(inputColor);
            

            //string toast = string.Format("{0} convertito in RGB", spinner.GetItemAtPosition(e.Position));
            //Toast.MakeText(this, toast, ToastLength.Short).Show();
        }
        private void spinnerAnimColor_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            if (spinner.GetItemAtPosition(e.Position).Equals("None"))
            {
                return;
            }
            //string toast = string.Format("The anim is {0}", spinner.GetItemAtPosition(e.Position));
            //Toast.MakeText(this, toast, ToastLength.Short).Show();
        }
        private void spinnerMicAnim_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            if (spinner.GetItemAtPosition(e.Position).Equals("None"))
            {
                return;
            }
            //string toast = string.Format("The anim is {0}", spinner.GetItemAtPosition(e.Position));
            //Toast.MakeText(this, toast, ToastLength.Short).Show();
        }
        public Color hexToRGB(String hexString)
        {
            if(!hexString.Contains("#"))
            {
                Console.WriteLine("HexString format error 1");
                debugTextView.Text = "Invalid color";
                return Color.White;
            }

            hexString = hexString.Substring(hexString.IndexOf("#"));
            if(hexString.Length!=7)
            {
                Console.WriteLine("HexString format error 2");
                debugTextView.Text = "Invalid color";
                return Color.White;
            }
            if(hexString.IndexOf("#")!=0)
            {
                Console.WriteLine("HexString format error 3");
                debugTextView.Text = "Invalid color";
                return Color.White;
            }
            hexString = hexString.Substring(1);
            int red=255, green=255, blue=255;
            try
            {
                red = Convert.ToInt32(hexString.Substring(0, 2), 16);
                green = Convert.ToInt32(hexString.Substring(2, 2), 16);
                blue = Convert.ToInt32(hexString.Substring(4, 2), 16);
            }
            catch(Exception e)
            {
                Console.WriteLine("HexString format error 4");
                debugTextView.Text = "Invalid color";
            }
            
            //Console.WriteLine(hexString);
            //Console.WriteLine("rgb:" + red + " " + green + " " + blue);
            return new Color(red, green, blue);
        }

        private void VerificaBluetoothAttivo()
        {

            mBluetoothAdapter = BluetoothAdapter.DefaultAdapter;


            if (!mBluetoothAdapter.Enable())
            {
                Toast.MakeText(this, "Bluetooth disattivato",
                    ToastLength.Short).Show();
            }

            if (mBluetoothAdapter == null)
            {
                Toast.MakeText(this,
                    "Bluetooth occupato o inesistente", ToastLength.Short)
                    .Show();
            }
        }
        public void ConnettiAdArduino()
        {
            // inizia la connessione con arduino
            System.Console.WriteLine("Connessione in corso ...");
            connStateTextView.Text = "Connessione in corso ...";
            BluetoothDevice device = mBluetoothAdapter.GetRemoteDevice(address);
            
            //indica all'adattatore che non siamo piu visibili
            mBluetoothAdapter.CancelDiscovery();
            try
            {
                //creo socket con arduino
                btSocket = device.CreateRfcommSocketToServiceRecord(MY_UUID);
                //connetto a socket
                btSocket.Connect();
                System.Console.WriteLine("Connessione avvenuta a socket arduino");
                connStateTextView.Text = "Connesso a " + device.Name;
            }
            catch (System.Exception e)
            {
                //errore di creazione del socket
                Console.WriteLine(e.Message);
                try
                {
                    btSocket.Close();
                }
                catch (System.Exception)
                {
                    System.Console.WriteLine("Impossibile connettersi");
                    debugTextView.Text = "Impossibile connettersi";
                }
                System.Console.WriteLine("Socket non creato");
                debugTextView.Text = "Socket non creato";
                connStateTextView.Text = "Disconnesso";
            }

        }
        public void DisconnettiDaArduino()
        {
            if (btSocket != null)
            {
                btSocket.Dispose();
                btSocket = null;
                System.Console.WriteLine("disconnesso da socket");
                connStateTextView.Text = "Disconnesso";
            }
            else
            {
                System.Console.WriteLine("Nessun socket da disconnettere");
                debugTextView.Text = "Nessun socket da disconnettere";
            }

        }
        private void writeData(Java.Lang.String data)
        {
            // ottengo stream da socket
            try
            {
                outStream = btSocket.OutputStream;
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine("errore nell'ottenere il socket" + e.Message);
                debugTextView.Text = "errore nell'ottenere il socket";
                return;
            }

            //creo stringa da inviare
            Java.Lang.String message = data;

            //converto in byte
            byte[] msgBuffer = message.GetBytes();

            try
            {
                // scrivo bytes sul buffer
                outStream.Write(msgBuffer, 0, msgBuffer.Length);
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine("errore nell'invio dei dati al socket" + e.Message);
                debugTextView.Text = "errore nell'invio dei dati al socket";
            }
            Console.WriteLine("inviato " + data);
            debugTextView.Text = "inviato con successo cmd: " + data;
        }

    }
}