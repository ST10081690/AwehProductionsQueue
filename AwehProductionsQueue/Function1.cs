using System;
using Microsoft.WindowsAzure.Storage;
using Azure.Storage.Queues;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Azure.Storage.Blobs;
using System.IO;
using System.Configuration;
using Microsoft.Azure.Storage.Blob;

namespace AwehProductionsQueue
{
    public class Function1
    {
        //variable to reference the connection string declared in local settings
        public static string connect = Environment.GetEnvironmentVariable("StorageConnect");

        //variable specifying the blob storage's name in MS Azure
        static string blobStore = "vaccination-storage";
        
    [FunctionName("VaccineQueueFunction")]
        //connecting function to azure queue and connecting a second queue that will store info after it was processed
        public void Run([QueueTrigger("vaccine-queue", Connection = "StorageConnect")] string newVaccineQueue,
                        [Queue("vaccine-details", Connection = "StorageConnect")] out string savedDetails, ILogger log)
        {
            //printing that queue was processed and the content of queue message
            log.LogInformation($"C# Queue trigger function processed: {newVaccineQueue}");

            //making second queue equal content processed from first queue
            savedDetails = newVaccineQueue;

            //calling method to save details to azure sql database
            SaveToContainer(savedDetails);
        }

        public static IActionResult SaveToContainer(string savedDetails)
        {
            //CREATING AND CONNECTING BLOB---------------//
            //creating container client to connect to establish a connection with the Azure Container
            BlobContainerClient vaccineBlob = new BlobContainerClient(connect, blobStore);

            //creating blob container if it does not exist
            vaccineBlob.CreateIfNotExists();

            //variable to specify directory path to the string that holds queue message
            string message = Path.GetFileName(savedDetails);


            //CHECKING MESSAGE FORMAT--------------------//
            //array separating message into different parts at every colon ":"
            string[] divideDetails = savedDetails.Split(':', StringSplitOptions.RemoveEmptyEntries);

            //integer to keep track of number of letter
            int letter = 0;

            //character array for second detail of message
            char[] lettersInDetail = divideDetails[1].ToCharArray();

            //loop to count the letters within the detail
            foreach (char digit in lettersInDetail)
            {
                //checking if character is a letter
                if (char.IsLetter(digit))
                {
                    //adding to the count
                    letter++;

                }
            }

            //checking if there are any letters in second property of message
            //if there are letters then it cannot be a date and must be a vaccination center
            if (letter > 0 && divideDetails.Length == 4)//message is in format 1
            {
                //creating blob client, specifying folder for corresponding format
                BlobClient vBlob1 = vaccineBlob.GetBlobClient("Format-1/" + message);

                //UPLOAD------//
                //uploading string(message) to blob storage
                vBlob1.Upload(BinaryData.FromString(savedDetails));
            }

            //checking there are NOT any letters in second property of message
            //if there are not letters then the second property must be a vaccination date
            //also checking if there are four (4) properties in message
            if(letter == 0 && divideDetails.Length == 4) //message is in format 2
            {
                //creating blob client, specifying folder for corresponding format
                BlobClient vBlob2 = vaccineBlob.GetBlobClient("Format-2/" + message);

                //UPLOAD------//
                //uploading string(message) to blob storage
                vBlob2.Upload(BinaryData.FromString(savedDetails));
            }

            //checking if there are only three properties in the message
            if (divideDetails.Length == 3) //message is in format 3
            {
                //creating blob client, specifying folder for corresponding format
                BlobClient vBlob3 = vaccineBlob.GetBlobClient("Format-3/" + message);

                //UPLOAD------//
                //uploading string(message) to blob storage
                vBlob3.Upload(BinaryData.FromString(savedDetails));
            }

            //returning success message
            return new OkObjectResult("Details were saved successfully");
        }
        //end of method
    }
    //END OF CLASS
}
//-------------------------------------------------------------END OF CODE---------------------------------------------------------------------//
