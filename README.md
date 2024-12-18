# Quickshare
Sharing a file has never been easier - simply upload files to the decentralised storage-network Storj DCS and share a link to the file!

![quickshare_with_shadow](https://user-images.githubusercontent.com/1833242/182858039-5c8a6ad8-2c2f-4341-865f-6f7179393666.png)

# Installation
This is a dotnet-tool! So assuming you have dotnet-SDK installed simply install the quickshare-tool by opening a command prompt:

``
dotnet tool install -g quickshare
``

This will install the tool globally so you can simply call ``quickshare`` from every location.

# Setup
Before uploading your first file you have to setup the tool by providing an access grant and a bucket name from your storj account. You get 25GB of free storage on https://storj.io. Follow this [guide](https://docs.storj.io/dcs/getting-started/quickstart-uplink-cli/uploading-your-first-object/create-first-access-grant/) on how to create an access grant.

Then setup quickshare like this:
``
quickshare config -g ACCESS_GRANT -b BUCKET_NAME
``

Replace ACCESS_GRANT and BUCKET_NAME with your values.

# Usage
To upload and share a file simply enter the following:

``
quickshare -f myAwesomeFile.txt
``

If you want the file to only be available for a certain amount of time adjust your prompt like this:

``
quickshare -f myAwesomeFile.txt -d "1 week"
``

This will make your file available for 1 week and delete it automatically afterwards, which will also render your share-URL obsolet. You may provide different phrases like "3 days", "1 week, 2 days" etc. to the "-d"-Parameter.

You may also upload the content of a directory:

``
quickshare sharefolder -r "c:\myFolder" -d "2 weeks"
``

This will upload all files in the folder "c:\myFolder" and create an index.html file that lists all files. The index.html file will be uploaded, too. The share-URL will point to the index.html file.

If you need the subfolders, too, user the "sub"-parameter:

``
quickshare sharefolder -s -r "c:\myFolder" -d "2 weeks"
``

I you only want to upload the files only without generating an index.html file, use the "UploadOnly"-parameter:

``
quickshare sharefolder -u -s -r "c:\myFolder" -d "2 weeks"
``