#!/bin/bash
# -*- ENCODING: UTF-8 -*-
while true
do
	java -Dlogback.configurationFile=logback.xml -Dfile.encoding=utf-8 -jar multi.jar -o true
done