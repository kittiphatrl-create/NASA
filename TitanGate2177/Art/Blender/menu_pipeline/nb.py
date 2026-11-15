# Small node-building helpers shared by the menu scripts.
import bpy, math


def srgb(h, mul=1.0):
    h = h.lstrip("#")
    out = []
    for i in (0, 2, 4):
        c = int(h[i:i + 2], 16) / 255.0
        c = c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
        out.append(c * mul)
    return tuple(out)


class NB:
    def __init__(self, nt):
        self.nt = nt

    def node(self, t, **props):
        n = self.nt.nodes.new(t)
        for k, v in props.items():
            setattr(n, k, v)
        return n

    def set(self, sock, v):
        if v is None:
            return
        if isinstance(v, bpy.types.NodeSocket):
            self.nt.links.new(v, sock)
            return
        if sock.type == "RGBA" and hasattr(v, "__len__") and len(v) == 3:
            v = (v[0], v[1], v[2], 1.0)
        sock.default_value = v

    def math(self, op, a, b=None, c=None, clamp=False):
        n = self.node("ShaderNodeMath", operation=op, use_clamp=clamp)
        self.set(n.inputs[0], a)
        if b is not None:
            self.set(n.inputs[1], b)
        if c is not None:
            self.set(n.inputs[2], c)
        return n.outputs[0]

    def vmath(self, op, a, b=None, scale=None):
        n = self.node("ShaderNodeVectorMath", operation=op)
        self.set(n.inputs[0], a)
        if b is not None:
            self.set(n.inputs[1], b)
        if scale is not None:
            self.set(n.inputs[3], scale)
        if op in ("DOT_PRODUCT", "LENGTH", "DISTANCE"):
            return n.outputs["Value"]
        return n.outputs["Vector"]

    def maprange(self, v, a, b, c, d, interp="LINEAR"):
        n = self.node("ShaderNodeMapRange", interpolation_type=interp, clamp=True)
        self.set(n.inputs[0], v)
        n.inputs[1].default_value = a
        n.inputs[2].default_value = b
        n.inputs[3].default_value = c
        n.inputs[4].default_value = d
        return n.outputs[0]

    def mix(self, fac, a, b, blend="MIX"):
        n = self.node("ShaderNodeMix", data_type="RGBA", blend_type=blend)
        n.clamp_factor = True
        self.set(n.inputs[0], fac)
        self.set(n.inputs[6], a)
        self.set(n.inputs[7], b)
        return n.outputs[2]

    def rgb(self, col):
        n = self.node("ShaderNodeRGB")
        n.outputs[0].default_value = (col[0], col[1], col[2], 1.0)
        return n.outputs[0]

    def vec(self, v):
        n = self.node("ShaderNodeCombineXYZ")
        for i in range(3):
            n.inputs[i].default_value = v[i]
        return n.outputs[0]

    def sep(self, v):
        n = self.node("ShaderNodeSeparateXYZ")
        self.set(n.inputs[0], v)
        return n.outputs

    def scale_color(self, col, s):
        return self.mix(1.0, col, (s, s, s), "MULTIPLY") if not isinstance(s, bpy.types.NodeSocket) else self._scale_sock(col, s)

    def _scale_sock(self, col, s):
        n = self.node("ShaderNodeVectorMath", operation="SCALE")
        self.set(n.inputs[0], col)
        self.set(n.inputs[3], s)
        return n.outputs["Vector"]
