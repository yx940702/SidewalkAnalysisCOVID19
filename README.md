# Grasshopper Plugin COVID19 for Sidewalk Analysis

![alt text](https://github.com/yx940702/SidewalkAnalysisCOVID19/blob/main/images/logo.png?raw=true)



**Research Question**

The goal of this plugin is to allow users to use an unmodified Rhino model from New York City’s Department of City Planning to evaluate sidewalk segments social distancing. Instead of providing an extremely accurate statistical model to estimate pedestrian traffic, the goal of this plugin is to provide a prototype with a reasonable approximation model, which could be switched out easily upon further studies.

There are several 3 intertwined core problems to solve: giving the model a data structure, estimating pedestrian count, and evaluating sidewalk segments. Answering these three problems provided the structure of the 3 components in this plugin.

**Workflow**

![alt_text](https://github.com/yx940702/SidewalkAnalysisCOVID19/blob/98f6454a74ec3f168011e42f87cd10fde2ef6c79/images/icons.png)


I worked out a working structure with native grasshopper components and then scripted in c# inside grasshopper, which allowed me to prototype and troubleshoot quickly. Then, I scripted the components in Visual Studio to produce the finished product.

**Component: Parse Data in Rhino Model**


![alt_text](https://github.com/yx940702/SidewalkAnalysisCOVID19/blob/025b02a216f2a89e4f6a3b7c52a204e4eeea1120/images/parsedata.png)


To provide a data structure, the model is parsed based on city blocks. Curves from Layer _Linework:Pavement Edge_ are used instead of _Linework:Sidewalk _because they include pocket plazas and other pedestrian areas that are not included in _Sidewalk. _These curves are joined to make Blocks.

Instead of closed breps, the provided model only includes planar surfaces. In order to provide opportunities to estimate floor area, the height of a building is required. Therefore, the roof surfaces from _Buildings:RoofTop Surface _are used to provide the height values (surface centroid’s z value). The roofs are parsed to different Blocks according to each surface’s centroid’s containment within a Block and are then extruded to the planes of corresponding Blocks to create Buildings. A list of Buildings on a given Block is stored in a data tree that corresponds to all the provided Blocks.

Lot Line curves from layer _Linework:Lot Lines_  are also taken in to provide a basis for making sidewalk segments and parsed based on their centroids’ containment within the Blocks. The curves provided are not very clean, most of them are closed curves but there are a few extremely short lines, which are weeded out to provide clean data for the next component.

**Component: Pedestrian Traffic Volume Analysis**


![alt_text](https://github.com/yx940702/SidewalkAnalysisCOVID19/blob/025b02a216f2a89e4f6a3b7c52a204e4eeea1120/images/Pedestrian%20traffic.png)


The parsed model from the first component is plugged into corresponding parameter inputs on this component.

To lighten the computation, not all parsed data from the first component are analyzed for pedestrian counts. Since the city blocks are not sorted in any way, it would be difficult to select different blocks based on index. So target blocks’ curves are put in to search for the matching Blocks in the input data. The match Blocks’ indexes are then used to call matching buildings and Lot Lines.

The Blocks and Lot Lines are used to create a sidewalk surface. This is the best approximation of a sidewalk surface segment given the model. Looking through Google Maps in Satellite View and Street View, I found out that the building footprints do not estimate the sidewalk boundaries well because there are fences and other data not included in the footprints.


![alt_text](https://github.com/yx940702/SidewalkAnalysisCOVID19/blob/025b02a216f2a89e4f6a3b7c52a204e4eeea1120/images/targetbuilding.png)


The buildings on a target Block are calculated for their volumes to estimate total floor area by dividing volumes with average floor heights. This floor area is then used to estimate the number of occupants on the Block by dividing it with average square footage per occupants.


![alt_text](https://github.com/yx940702/SidewalkAnalysisCOVID19/blob/025b02a216f2a89e4f6a3b7c52a204e4eeea1120/images/radius.png)


An affecting radius (r) is used to circumscribe a surrounding environment of a given Block. This environment includes nearby buildings, subway stops (optional), interest points (optional) within the radius. Each item in this environment is given a probability inversely proportional to its distance (d) to the target Block with the linear function (r-d)/r to represent the decreasing probability of a person at an item to use the sidewalk as the person is further away.

The occupants in the buildings on a Block and its surrounding blocks are then overlaid with corresponding percentages, which represent the percentages of occupants are likely to be on a sidewalk segment at a given time. These percentages can be changed to reflect different time of the day: they might be very high at peak hours and low throughout the day.

The pedestrian count is estimated with all the data calculated above with a simple statistical model that could be changed easily in the future.

The data pertinent to the target Blocks such as pedestrian count, blocks curves, building breps, sidewalk surfaces, and buildings, subway stops, and interest points within the radius are provided to the next component

**Component: Social Distance Analysis**


![alt_text](https://github.com/yx940702/SidewalkAnalysisCOVID19/blob/025b02a216f2a89e4f6a3b7c52a204e4eeea1120/images/socialdistance.png)


This component uses the sidewalk surfaces and pedestrian count to calculate distances between randomized points on a given sidewalk segment. The randomization can be polynomially weighted according to given attraction points such as subway stops and interest points to represent the increased likelihood of a person at a part of the sidewalk closer to the attraction points. In comparison to circle packing, this randomization method is a better representation of people on the sidewalk since people are not maximizing effort to distance from each other at a given distance as they would be in circle packing. The pedestrian count can be manually input for special circumstances such as events.

The component also intakes grid size (in ft) and iteration to adjust the refinement of the calculation , allowing users to choose on a spectrum between a draft quality (faster and less accurate) and a presentation quality (slower and more accurate) calculation.



![alt_text](https://github.com/yx940702/SidewalkAnalysisCOVID19/blob/025b02a216f2a89e4f6a3b7c52a204e4eeea1120/images/colormesh.png)
Color Map of Social Distance



![alt_text](https://github.com/yx940702/SidewalkAnalysisCOVID19/blob/025b02a216f2a89e4f6a3b7c52a204e4eeea1120/images/problem.png)
Problem Areas



This calculation is visualized in color and mapped onto a mesh that represents the sidewalk. Green (0,255,0) represents areas where people’s distances are exactly at the desired distance. The higher the r value is in the color, the less distance people have than the desirable distance in an area. The higher the blue value is in the color, the more distance people have than the desirable distance in an area. The distance data is mapped onto the domain (0, 2*desired distance).  Areas where social distancing is less possible are recorded in collision and represented by colored mesh in the Problem Area. 

**Conclusion**

This plugin is designed to be compartmentalized, allowing users to pick and choose different components in different circumstances. The compartmentalization also offers further expansion into other usage in spatial analysis, eases troubleshooting, and lower crash risk. The visualization can aid architects and urban designers to choose building locations, density, size of sidewalk and pocket plaza, circulation pattern, entrance location, loading dock location, and so on.
