# SidewalkAnalysisCOVID19


<p style="color: red; font-weight: bold">>>>>>  gd2md-html alert:  ERRORs: 0; WARNINGs: 0; ALERTS: 8.</p>
<ul style="color: red; font-weight: bold"><li>See top comment block for details on ERRORs and WARNINGs. <li>In the converted Markdown or HTML, search for inline alerts that start with >>>>>  gd2md-html alert:  for specific instances that need correction.</ul>

<p style="color: red; font-weight: bold">Links to alert messages:</p><a href="#gdcalert1">alert1</a>
<a href="#gdcalert2">alert2</a>
<a href="#gdcalert3">alert3</a>
<a href="#gdcalert4">alert4</a>
<a href="#gdcalert5">alert5</a>
<a href="#gdcalert6">alert6</a>
<a href="#gdcalert7">alert7</a>
<a href="#gdcalert8">alert8</a>

<p style="color: red; font-weight: bold">>>>>> PLEASE check and correct alert issues and delete this message and the inline alerts.<hr></p>


Grasshopper Plugin COVID19 for Sidewalk Analysis



<p id="gdcalert1" ><span style="color: red; font-weight: bold">>>>>>  gd2md-html alert: inline image link here (to images/image1.png). Store image on your image server and adjust path/filename/extension if necessary. </span><br>(<a href="#">Back to top</a>)(<a href="#gdcalert2">Next alert</a>)<br><span style="color: red; font-weight: bold">>>>>> </span></p>


![alt_text](images/image1.png "image_tooltip")


**Research Question**

The goal of this plugin is to allow users to use an unmodified Rhino model from New York City’s Department of City Planning to evaluate sidewalk segments social distancing. Instead of providing an extremely accurate statistical model to estimate pedestrian traffic, the goal of this plugin is to provide a prototype with a reasonable approximation model, which the approximation model could be switched out easily upon further studies.

There are several 3 intertwined core problems to solve: giving the model a data structure, estimating pedestrian count, and evaluating sidewalk segments. Answering these three problems provided the structure of the 3 components in this plugin.

**Workflow**

I worked out a working structure with native grasshopper components and then scripted in c# inside grasshopper, which allowed me to prototype and troubleshoot quickly. Then, I scripted the components in Visual Studio to produce the finished product.

**Component: Parse Data in Rhino Model**



<p id="gdcalert2" ><span style="color: red; font-weight: bold">>>>>>  gd2md-html alert: inline image link here (to images/image2.png). Store image on your image server and adjust path/filename/extension if necessary. </span><br>(<a href="#">Back to top</a>)(<a href="#gdcalert3">Next alert</a>)<br><span style="color: red; font-weight: bold">>>>>> </span></p>


![alt_text](images/image2.png "image_tooltip")


To provide a data structure, the model is parsed based on city blocks. Curves from Layer _Linework:Pavement Edge_ are used instead of _Linework:Sidewalk _because they include pocket plazas and other pedestrian areas that are not included in _Sidewalk. _These curves are joined to make Blocks.

Instead of closed breps, the provided model only includes planar surfaces. In order to provide opportunities to estimate floor area, the height of a building is required. Therefore, the roof surfaces from _Buildings:RoofTop Surface _are used to provide the height values (surface centroid’s z value). The roofs are parsed to different Blocks according to each surface’s centroid’s containment within a Block and are then extruded to the planes of corresponding Blocks to create Buildings. A list of Buildings on a given Block is stored in a data tree that corresponds to all the provided Blocks.

Lot Line curves from layer _Linework:Lot Lines_  are also taken in to provide a basis for making sidewalk segments and parsed based on their centroids’ containment within the Blocks. The curves provided are not very clean, most of them are closed curves but there are a few extremely short lines, which are weeded out to provide clean data for the next component.

**Component: Pedestrian Traffic Volume Analysis**



<p id="gdcalert3" ><span style="color: red; font-weight: bold">>>>>>  gd2md-html alert: inline image link here (to images/image3.png). Store image on your image server and adjust path/filename/extension if necessary. </span><br>(<a href="#">Back to top</a>)(<a href="#gdcalert4">Next alert</a>)<br><span style="color: red; font-weight: bold">>>>>> </span></p>


![alt_text](images/image3.png "image_tooltip")


The parsed model from the first component is plugged into corresponding parameter inputs on this component.

To lighten the computation, not all parsed data from the first component are analyzed for pedestrian counts. Since the city blocks are not sorted in any way, it would be difficult to select different blocks based on index. So target blocks’ curves are put in to search for the matching Blocks in the input data. The match Blocks’ indexes are then used to call matching buildings and Lot Lines.

The Blocks and Lot Lines are used to create a sidewalk surface. This is the best approximation of a sidewalk surface segment given the model. Looking through Google Maps in Satellite View and Street View, I found out that the building footprints do not estimate the sidewalk boundaries well because there are fences and other data not included in the footprints.



<p id="gdcalert4" ><span style="color: red; font-weight: bold">>>>>>  gd2md-html alert: inline image link here (to images/image4.png). Store image on your image server and adjust path/filename/extension if necessary. </span><br>(<a href="#">Back to top</a>)(<a href="#gdcalert5">Next alert</a>)<br><span style="color: red; font-weight: bold">>>>>> </span></p>


![alt_text](images/image4.png "image_tooltip")


The buildings on a target Block are calculated for their volumes to estimate total floor area by dividing volumes with average floor heights. This floor area is then used to estimate the number of occupants on the Block by dividing it with average square footage per occupants.



<p id="gdcalert5" ><span style="color: red; font-weight: bold">>>>>>  gd2md-html alert: inline image link here (to images/image5.png). Store image on your image server and adjust path/filename/extension if necessary. </span><br>(<a href="#">Back to top</a>)(<a href="#gdcalert6">Next alert</a>)<br><span style="color: red; font-weight: bold">>>>>> </span></p>


![alt_text](images/image5.png "image_tooltip")


An affecting radius (r) is used to circumscribe a surrounding environment of a given Block. This environment includes nearby buildings, subway stops (optional), interest points (optional) within the radius. Each item in this environment is given a probability inversely proportional to its distance (d) to the target Block with the linear function (r-d)/r to represent the decreasing probability of a person at an item to use the sidewalk as the person is further away.

The occupants in the buildings on a Block and its surrounding blocks are then overlaid with corresponding percentages, which represent the percentages of occupants are likely to be on a sidewalk segment at a given time.

The pedestrian count is estimated with all the data calculated above with a simple statistical model that could be changed easily in the future.

The data pertinent to the target Blocks such as pedestrian count, blocks curves, building breps, sidewalk surfaces, and buildings, subway stops, and interest points within the radius are provided to the next component

**Component: Social Distance Analysis**



<p id="gdcalert6" ><span style="color: red; font-weight: bold">>>>>>  gd2md-html alert: inline image link here (to images/image6.png). Store image on your image server and adjust path/filename/extension if necessary. </span><br>(<a href="#">Back to top</a>)(<a href="#gdcalert7">Next alert</a>)<br><span style="color: red; font-weight: bold">>>>>> </span></p>


![alt_text](images/image6.png "image_tooltip")


This component uses the sidewalk surfaces and pedestrian count to calculate distances between randomized points on a given sidewalk segment. The randomization can be polynomially weighted according to given attraction points such as subway stops and interest points to represent the increased likelihood of a person at a part of the sidewalk closer to the attraction points. In comparison to circle packing, this randomization method is a better representation of people on the sidewalk since people are not maximizing effort to distance from each other at a given distance as they would be in circle packing.

The component also intakes grid size (in ft) and iteration to adjust the refinement of the calculation , allowing users to choose on a spectrum between a draft quality (faster and less accurate) and a presentation quality (slower and more accurate) calculation.



<p id="gdcalert7" ><span style="color: red; font-weight: bold">>>>>>  gd2md-html alert: inline image link here (to images/image7.png). Store image on your image server and adjust path/filename/extension if necessary. </span><br>(<a href="#">Back to top</a>)(<a href="#gdcalert8">Next alert</a>)<br><span style="color: red; font-weight: bold">>>>>> </span></p>


![alt_text](images/image7.png "image_tooltip")


Color Map of Social Distance



<p id="gdcalert8" ><span style="color: red; font-weight: bold">>>>>>  gd2md-html alert: inline image link here (to images/image8.png). Store image on your image server and adjust path/filename/extension if necessary. </span><br>(<a href="#">Back to top</a>)(<a href="#gdcalert9">Next alert</a>)<br><span style="color: red; font-weight: bold">>>>>> </span></p>


![alt_text](images/image8.png "image_tooltip")


Problem Areas

This calculation is visualized in color and mapped onto a mesh that represents the sidewalk. Green (0,255,0) represents areas where people’s distances are exactly at the desired distance. The higher the r value is in the color, the less distance people have than the desirable distance in an area. The higher the blue value is in the color, the more distance people have than the desirable distance in an area. The distance data is mapped onto the domain (0, 2*desired distance).  Areas where social distancing is less possible are recorded in collision and represented by colored mesh in the Problem Area. 
