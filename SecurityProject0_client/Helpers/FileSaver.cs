using Microsoft.Toolkit.Uwp.Helpers;
using SecurityProject0_shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace SecurityProject0_client.Helpers
{
    static class FileSaver
    {
        public async static void SaveFile(File f)
        {
            var temp = ApplicationData.Current.TemporaryFolder;
            var storage = await temp.CreateFileAsync(f.Name, CreationCollisionOption.GenerateUniqueName);
            f.Path = storage.Path;
            var bytes = Convert.FromBase64String(f.RawMessage);
            using (var stream = await storage.OpenTransactedWriteAsync())
            {
                using (var dataWriter = new DataWriter(stream.Stream))
                {
                    dataWriter.WriteBytes(bytes);
                    stream.Stream.Size = await dataWriter.StoreAsync();
                    await stream.CommitAsync();
                }
            }
            f._rawMessage = "";



                //await FileIO.WriteTextAsync(storage, f.RawMessage, Windows.Storage.Streams.UnicodeEncoding.Utf8);
        }
    }
}
