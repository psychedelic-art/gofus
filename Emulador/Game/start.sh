#!/bin/bash
# -*- ENCODING: UTF-8 -*-
while true
do
	java -Dlogback.configurationFile=logback.xml -Dfile.encoding=utf-8 -jar -Xmx2G game.jar -o true
done