cd ..\..\..\Stickers

magick mogrify -trim +repage *.png 
magick mogrify -resize 512x512 *.png 

pause