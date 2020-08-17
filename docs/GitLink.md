GitHub Link
===
Create a copy of this repository in order to play around with it. We will first create a clone of this repo. Then link this clone to a new repo of your own and finally connect **CoFlows** with **GitHub** to automate deployment:
* Clone this project to your local environment:  
  `git clone https://github.com/CoFlows/CoFlows-Public.git`
* In the unzipped folder CoFlows-Public, delete the .git/ folder:  
  `Bash> rm -rf .git`
* Create your own new cloud repository on **GitHub**
* Link your local copy to your new cloud repository:  
  `git init`  
  `git add .`  
  `git commit -a -m "first commit"`  
  `git remote add origin https://github.com/CoFlows/CoFlows-Public.git`  
  `git push (--force) origin master`  

We must now both get a **CoFlows** key and a **GitHub** token in order to allow CoFlows to deploy new changes to the GiHub repo after every commit.

#### CoFlows Key
* Login to **CoFlows**
* Click on your name at the top left corner
* Click on Profile and copy the Key {CoFlowsKey}

#### GitHub Access Token:
* Login to **GitHub**
* Goto USER
    * Developer Settings
    * Personal Access Tokens  
      Note: coflows.quant.app  
      Repo: tick all  
      Copy token = {GitHubToken}  

#### Link the GitHub repo to CoFlows
In the **GitHub** page goto
* Click on _Settings_ and then _Webhooks_
* Click on _Add Webhooks_ and make sure the options look as follows:  
  Payload URL: https://coflows.quant.app/flow/githubpost?key={CoFlowsKey}&token={GitHubToken}  
  Content Type: application/json  
  Secret: blank  
  SSL: enable  
  Trigger: push event  
  Active: yes  

Next let's discuss the [CoFlows Environment](Environment.md "Environment")