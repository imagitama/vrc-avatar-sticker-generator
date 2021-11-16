cd ..\..\..\Stickers

magick mogrify -bordercolor none -border 2x2 -background white -alpha background -channel A -blur 0x2 -level 0,100% *.png
magick mogrify -trim +repage *.png 
magick mogrify -resize 512x512 *.png 

pause