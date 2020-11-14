using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Lab5.Data;
using Lab5.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab5.Controllers
{
    public class AnswerImagesController : Controller
    {
        private readonly AnswerImageDataContext _context;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string containerName = "answerimages";

        public AnswerImagesController(AnswerImageDataContext context, BlobServiceClient blobServiceClient)
        {
            _context = context;
            _blobServiceClient = blobServiceClient;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.AnswerImages.ToListAsync());
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile answerImage)
        {
            BlobContainerClient containerClient; // instanciate a class to allow us to manipulate the blob storage we made

            try
            {
                // Attempt to create a new container that will hold our blob images if it already exists we'll get a RequestFailedException
                containerClient = await _blobServiceClient.CreateBlobContainerAsync(containerName); 
                // Set the access to the container to be public allowing anyone to upload images to our blob container
                containerClient.SetAccessPolicy(Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer);
            } catch(RequestFailedException)
            {
                // We've already made a blob container, continue with the old blob container.
                containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            }

            try
            {
                // create the blob to hold the data
                var blockBlob = containerClient.GetBlobClient(answerImage.FileName);

                // check if the blob already exists
                if (await blockBlob.ExistsAsync())
                {
                    // if so lets delete it.
                    await blockBlob.DeleteAsync();
                }

                // get a new memory stream so we can upload to the storage container.
                using (var memoryStream = new MemoryStream())
                {
                    // copy the file data into memory
                    await answerImage.CopyToAsync(memoryStream);

                    // navigate back to the beginning of the memory stream
                    memoryStream.Position = 0;

                    // send the file to the cloud
                    await blockBlob.UploadAsync(memoryStream);
                    memoryStream.Close();
                }

                // add the photo to the database if it uploaded successfully.
                var image = new AnswerImage();
                image.Url = blockBlob.Uri.AbsoluteUri;
                image.FileName = answerImage.FileName;

                // add it to our array of answer images in our AnswerImageDataContext
                _context.AnswerImages.Add(image);
                // update our data in the database with the new data added above.
                _context.SaveChanges();
            }
            catch (RequestFailedException)
            {
                // if the image fails to upload to our storage.
                View("Error");
            }
            // return to our index view.
            return RedirectToAction("Index");


        }


        public async Task<IActionResult> Delete(int? id)
        {
            // User error checking to see if there actually is an id
            if (id == null)
            {
                return NotFound();
            }
            // find the image in our database
            var image = await _context.AnswerImages
                .FirstOrDefaultAsync(m => m.AnswerImageID == id);
            // User error checking to see if a user maliciously sends in an id that doesn't exist in our database
            // or if two users try to delete the image at the same time.
            if (image == null)
            {
                return NotFound();
            }
            // Send the image to the view for display.
            return View(image);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var image = await _context.AnswerImages.FindAsync(id);


            BlobContainerClient containerClient;
            // Get the container for our blobs, if failure go to Error view.
            try
            {
                containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            }
            catch (RequestFailedException)
            {
                return View("Error");
            }

            try
            {
                // Get the blob that holds the data
                var blockBlob = containerClient.GetBlobClient(image.FileName);

                // Check if the blob exists
                if (await blockBlob.ExistsAsync())
                {
                    // if the blob already exists delete it.
                    await blockBlob.DeleteAsync();
                }
                // Remove the image from our database
                _context.AnswerImages.Remove(image);
                // Save our changes
                await _context.SaveChangesAsync();

            }
            catch (RequestFailedException)
            {
                return View("Error");
            }
            // Go back to the index.
            return RedirectToAction("Index");
        }

    }

}
