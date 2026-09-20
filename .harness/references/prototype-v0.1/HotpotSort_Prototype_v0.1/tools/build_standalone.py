"""Bundle local CSS/JS into a single offline HTML. Python stdlib only."""
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[1]
web=ROOT/'Web'
text=(web/'index.html').read_text(encoding='utf-8')
text=re.sub(r'<link rel="stylesheet" href="([^"]+)">',lambda m:'<style>\n'+(web/m[1]).read_text(encoding='utf-8')+'\n</style>',text)
text=re.sub(r'<script src="([^"]+)"></script>',lambda m:'<script>\n'+(web/m[1]).read_text(encoding='utf-8').replace('</script','<\\/script')+'\n</script>',text)
(web/'standalone.html').write_text(text,encoding='utf-8')
print('Built Web/standalone.html:',len(text.encode('utf-8')),'bytes')
