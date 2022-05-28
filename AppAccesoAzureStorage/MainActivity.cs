using Android.App;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using Android.Widget;
using Plugin.Media;
using System;
using System.IO;
using Plugin.CurrentActivity;
using Android.Graphics;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AppDatosEImagen
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        string Archivo;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            SupportActionBar.Hide();
            CrossCurrentActivity.Current.Init(this, savedInstanceState);
            var Imagen = FindViewById<ImageView>(Resource.Id.imagen);
            var btnAlmacenar = FindViewById<Button>(Resource.Id.btnAlmacenar);
            var txtNombre = FindViewById<EditText>(Resource.Id.txtNombre);
            var txtCarrera = FindViewById<EditText>(Resource.Id.txtCarrera);
            var txtSemestre = FindViewById<EditText>(Resource.Id.txtSemestre);
            var txtSaldo = FindViewById<EditText>(Resource.Id.txtSaldo);
            Imagen.Click += async delegate
            {
                await CrossMedia.Current.Initialize();
                var archivo = await CrossMedia.Current.TakePhotoAsync(
                    new Plugin.Media.Abstractions.StoreCameraMediaOptions
                    {
                        Directory = "Imagenes",
                        Name = txtNombre.Text,
                        SaveToAlbum = true,
                        CompressionQuality = 50,
                        CustomPhotoSize = 30,
                        PhotoSize = Plugin.Media.Abstractions.PhotoSize.Medium,
                        DefaultCamera = Plugin.Media.Abstractions.CameraDevice.Front
                    });
                if(archivo == null)
                return;

                Bitmap bm = BitmapFactory.DecodeStream(archivo.GetStream());
                Archivo = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), txtNombre.Text + ".jpg");
                var stream = new FileStream(Archivo, FileMode.Create);
                bm.Compress(Bitmap.CompressFormat.Jpeg, 30, stream);
                stream.Close();
                Imagen.SetImageBitmap(bm);
               
            };
            btnAlmacenar.Click += async delegate
            {
                try
                {
                    var CuentaDeAlmacenamiento = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=datosdeclasehector;AccountKey=wZzgCiEz9toV1TrTao/7ol5OuScYJNBwoGsVqPct0zjdq3pNhBI68kOLk/3Tx/BXuJr0q/9Wa3u4jhJ3SEeYVQ==;EndpointSuffix=core.windows.net");
                    var clienteBlob = CuentaDeAlmacenamiento.CreateCloudBlobClient();
                    var Carpeta = clienteBlob.GetContainerReference("imagenes");
                    var resourceBlob = Carpeta.GetBlockBlobReference(txtNombre.Text + ".jpg");
                    resourceBlob.Properties.ContentType = "image/jpeg";


                    await resourceBlob.UploadFromFileAsync(Archivo.ToString());
                    
                   


                    Toast.MakeText(this, "Imagen Almacenda en contener de azure", ToastLength.Long).Show();
                    var TablaNoSQl = CuentaDeAlmacenamiento.CreateCloudTableClient();
                    var Coleccion = TablaNoSQl.GetTableReference("alumnosImagen");
                    await Coleccion.CreateIfNotExistsAsync();
                    var alumno = new Alumnos("Alumnosimagen", txtNombre.Text);
                    alumno.Carrera = txtCarrera.Text;
                    alumno.Semestre = txtSemestre.Text;
                    alumno.Saldo = double.Parse(txtSaldo.Text);
                    alumno.Imagen = "https://datosdeclasehector.blob.core.windows.net/imagenes/"+ txtNombre.Text + ".jpg";
                    var Store = TableOperation.Insert(alumno);
                    await Coleccion.ExecuteAsync(Store);
                    Toast.MakeText(this, "Datos almacenados en Alumnos Imagen", ToastLength.Long).Show();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                }
            };
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
    public class Alumnos : TableEntity
    {
        public Alumnos (string Categoria, string Nombre)
        {
            PartitionKey = Categoria;
            RowKey = Nombre;
        }
        public string Nombre { get; set; }
        public string Carrera { get; set; }
        public string  Semestre { get; set; }
        public double Saldo { get; set; }
        public string Imagen { get; set; }
    }
}