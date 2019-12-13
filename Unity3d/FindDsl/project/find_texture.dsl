input("*.tga","*.png","*.jpg","*.exr")
{
	int("maxSize",256){
		range(1,1024);
	};
	string("prop",""){
		multiple(["readable","mipmap"],[1,2]);
	};
	stringlist("filter", "");
	stringlist("notfilter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Textures");
	feature("description", "just so so");
}
filter
{
    if(stringcontains(assetpath, filter) && stringnotcontains(assetpath, notfilter)){
    	var(0) = loadasset(assetpath);
    	if(isnull(var(0))){
    		0;
    	} else {
    		var(1) = var(0).width;
    		var(2) = var(0).height;
    		var(3) = importer.isReadable;
    		var(4) = importer.mipmapEnabled;
    		//unloadasset(var(0));
    		order = var(1) < var(2) ? var(2) : var(1);
    		if((var(1) > maxSize || var(2) > maxSize) && (prop.Contains("1") && var(3) || !prop.Contains("1")) && (prop.Contains("2") && var(4) || !prop.Contains("2"))){
    			info = format("size:{0},{1} readable:{2} mipmap:{3}", var(1), var(2), var(3), var(4));
    			1;
    		} else {
    			0;
    		};
    	};
    }else{
        0;  
    };
};