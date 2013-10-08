Cloud Web Portal is: A Web Portal for MultiCloud Management

# Abstract
Cloud computing gives the individuals, researchers and developers a set of services that make their products available online very quickly without compromising the quality. Cloud Web Portal (CWP) is an open source Cloud management platform for the researchers and developers to test and produce their work/research in a usable, fast and easy to understand graphical user interface to proof a research concept, test a code or move a product to the production phase very rapidly. Aneka has been used in CWP as its framework, which will allow the developers to use a set of .Net-based APIs for monitoring, billing/accounting, scheduling and provisioning local desktop PCs, private, public or hybrid cloud. 80 experiments have been done using three different parameters proven that Aneka scheduling algorithm perform very efficiently for executing tasks in distributed machines, especially when the number of workers is increased. Surprisingly, with all the network latency and overhead to send and receive data, the 49 image tasks do not have significant effect on Aneka performance.


# CLOUDS Lab
CWP has been developed initially by me, and the copyright of CWP goes to CLOUDS Lab at The University of Melbourne. See this page from CLOUDS Lab official website: http://www.cloudbus.org/cwp/

#Software License
The CWP software is released as open source under the Apache License, Version 2.0.

# Note
You have to download the "Constellation complete admin skin" theme from
   <http://themeforest.net/item/constellation-complete-admin-skin/116461>
   and place the theme folders as following:
   - "js" folder in \CloudWebPortal\Scripts
   - "images" folder in \CloudWebPortal\Content
   - "css" folder in \CloudWebPortal\Content

# Installtion Guide
1. Install MVC3: http://www.asp.net/mvc/mvc3
2. Install NuGet Package Manager: http://nuget.org/
3. Download DataAnnotationsExtensions.MVC3 & EntityFramework by running the following command
in Package Manager Console <http://docs.nuget.org/docs/start-here/using-the-package-manager-console>:
	- Install-Package DataAnnotationsExtensions.MVC3
	- Install-Package EntityFramework
