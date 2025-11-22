<?php
        $files				= (isset($_REQUEST["f"])) ? $_REQUEST["f"] : "";
	$language 			= (isset($_REQUEST["l"])) ? $_REQUEST["l"] : "fr";
	$flashLanguage	 	= (isset($_REQUEST["fl"])) ? $_REQUEST["fl"] : "fr";
	
	include "versions_" . $language . ".php";

	$aFiles = explode("|", $files);
	$str = "";
	
	for($i = 0; $i < count($aFiles); $i++)
	{
		$aFileParams = explode(",",$aFiles[$i]);
		$sFileName = $aFileParams[0];
		$sFileVersion = $aFileParams[1];
		$sFileString = $sFileName;	
		
		if ($sFileVersion != $VERSIONS[$sFileString] || $flashLanguage != $language)
		{	
			if($str != "") $str .= "|";		
				$str .= $sFileName .",". $flashLanguage .",". $VERSIONS[$sFileString];
		}	
	}
	echo "&f=" . $str."&";
?>