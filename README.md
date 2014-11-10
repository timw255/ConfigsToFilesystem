#ConfigsToFilesystem

This script will help you pull configurations from the Sitefinity database and place them back in the file system. This script can be run on Sitefinity instances where the configurations are currently stored in the database. Meaning ```<sitefinityConfig storageMode="Database" />``` can be found in the web.config.

##What it does

* Backs up current configuration files to _**~/App_Data/Sitefinity/Configuration/_backup-48A004E0-DD62-48BE-ABCB-CC44F79A6126/**_
* Pulls Sitefinity configuration data from ```[sf_xml_config_items]``` and writes that information to _**~/App_Data/Sitefinity/Configuration/**_. (files that already exist in this folder are overwritten.)
* Attempts to merge the output from the database with any files that existed prior to script execution. The resulting output is saved to _**~/App_Data/Sitefinity/Configuration/_merged-48A004E0-DD62-48BE-ABCB-CC44F79A6126/**_

##Copying the source files

Merge the source files into your project.

##Running the script

_Make sure to do a backup of your project (including the database!) before attempting this procedure_

1. In your browser, navigate to the ConfigsToDatabase.aspx page.
2. Copy the contents of _**~/App_Data/Sitefinity/Configuration/_merged-48A004E0-DD62-48BE-ABCB-CC44F79A6126/**_ to _**~/App_Data/Sitefinity/Configuration/**_, overwriting any files that already exist.
3. Comment or delete ```<sitefinityConfig storageMode="Database" />``` from your web.config.
4. Restart the application pool for your website.
5. Navigate to the homepage of your site.
 
##Cleaning up

1. Delete the script.
2. Delete all rows from ```[sf_xml_config_items]```

Optionally, after verifying the functionality of the site, it's safe to delete the generated files and folders from your configuration directory.