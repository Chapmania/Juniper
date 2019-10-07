using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Juniper.GIS.Google.Geocoding;
using Juniper.GIS.Google.StreetView;

namespace Juniper.GoogleMaps
{
    public partial class ImageViewer : Form
    {
        public ImageViewer()
        {
            InitializeComponent();
        }

        public void SetImage(MetadataResponse metadata, GeocodingResponse geocode, Image image)
        {
            var address = (from result in geocode.results
                           orderby result.formatted_address.Length descending
                           select result.formatted_address)
                        .FirstOrDefault();
            SetControls(metadata, image, address);
        }

        private void SetControls(MetadataResponse metadata, Image image, string address)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<MetadataResponse, Image, string>(SetControls), metadata, image, address);
            }
            else
            {
                locationTextBox.Text = address ?? string.Empty;
                panoTextbox.Text = metadata.pano_id.ToString();
                latLngTextbox.Text = metadata.location.ToString();
                cubeMapPictureBox.Image?.Dispose();
                cubeMapPictureBox.Image = image;
            }
        }

        public void SetError(Exception exp = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<Exception>(SetError), exp);
            }
            else
            {
                var msg = exp?.Message ?? "ERROR";

                if (panoTextbox.Text.Length == 0)
                {
                    panoTextbox.Text = msg;
                }

                if (latLngTextbox.Text.Length == 0)
                {
                    latLngTextbox.Text = msg;
                }

                if (locationTextBox.Text.Length == 0
                    || (panoTextbox.Text.Length > 0
                        && latLngTextbox.Text.Length > 0))
                {
                    locationTextBox.Text = msg;
                }

                cubeMapPictureBox.Image?.Dispose();
                cubeMapPictureBox.Image = null;
            }
        }

        public event EventHandler<string> LocationSubmitted;

        private void LocationTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                locationTextBox.Text = locationTextBox.Text.Trim();
                panoTextbox.Text = string.Empty;
                latLngTextbox.Text = string.Empty;
                if (locationTextBox.Text.Length > 0)
                {
                    LocationSubmitted?.Invoke(this, locationTextBox.Text);
                }
            }
        }

        public event EventHandler<string> PanoSubmitted;

        private void PanoTextbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                panoTextbox.Text = panoTextbox.Text.Trim();
                latLngTextbox.Text = string.Empty;
                locationTextBox.Text = string.Empty;
                if (panoTextbox.Text.Length > 0)
                {
                    PanoSubmitted?.Invoke(this, panoTextbox.Text);
                }
            }
        }

        public event EventHandler<string> LatLngSubmitted;

        private void LatLngTextbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                latLngTextbox.Text = latLngTextbox.Text.Trim();
                panoTextbox.Text = string.Empty;
                locationTextBox.Text = string.Empty;
                if (latLngTextbox.Text.Length > 0)
                {
                    LatLngSubmitted?.Invoke(this, latLngTextbox.Text);
                }
            }
        }
    }
}