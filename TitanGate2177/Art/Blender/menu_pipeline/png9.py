import sys, os
import OpenImageIO as oiio
for p in sys.argv[sys.argv.index("--") + 1:]:
    before = os.path.getsize(p)
    buf = oiio.ImageBuf(p)
    spec = buf.spec()
    buf.specmod().attribute("png:compressionLevel", 9)
    tmp = p + ".tmp.png"
    ok = buf.write(tmp, "uint8")
    if ok and os.path.getsize(tmp) < before:
        os.replace(tmp, p)
    elif os.path.exists(tmp):
        os.remove(tmp)
    print("PNG9", os.path.basename(p), before, "->", os.path.getsize(p), spec.width, spec.height, spec.nchannels)
