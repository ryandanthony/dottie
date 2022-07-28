namespace dottie.Config;

// ReSharper disable once ClassNeverInstantiated.Global
public class AptSource
{
    public string Name { get; set; }
    //https://packages.microsoft.com/repos/azure-cli/
    public string RepositoryUrl { get; set; }
    //https://packages.microsoft.com/keys/microsoft.asc
    public string SigningKeyUrl { get; set; }
    
    /*
     *
     * 
     */
    
    public string Distribution { get; set; }
    public string[] Components { get; set; }
    
    
    
    
    /*
     * curl -sL https://packages.microsoft.com/keys/microsoft.asc |
     * gpg --dearmor |
     * sudo tee /etc/apt/trusted.gpg.d/microsoft.gpg > /dev/null
     * sudo apt-get update
     * sudo apt-get install ca-certificates curl apt-transport-https lsb-release gnupg
     */
    
}
/*
# /usr/bin/env bash
set -e
if [ -f /usr/share/keyrings/virtualbox-archive-keyring.gpg ]
then
    echo "virtualbox gpg already installed"       
else
    curl https://www.virtualbox.org/download/oracle_vbox_2016.asc | gpg --dearmor > virtualbox.gpg
    install -o root -g root -m 644 virtualbox.gpg /usr/share/keyrings/virtualbox-archive-keyring.gpg
    rm virtualbox.gpg
fi

if [ -f /etc/apt/sources.list.d/virtualbox.list ]
then
    echo "virtualbox source list already installed"    
else
    
    sh -c 'echo "deb [arch=amd64 signed-by=/usr/share/keyrings/virtualbox-archive-keyring.gpg] https://download.virtualbox.org/virtualbox/debian $(lsb_release -cs) contrib" > /etc/apt/sources.list.d/virtualbox.list'
fi


*/