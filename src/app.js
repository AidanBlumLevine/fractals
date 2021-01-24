import vert from './generalVert.vs';
import frag from './editFrag.fs';
import { vec3 } from 'gl-matrix';

var lastX, lastY, mouseDown = false;
var yawAngle = 0, pitchAngle = 0;
var playerPos = vec3.fromValues(0, 0, -4);
var move = [0, 0, 0];
var speed = 3;

var canvas = $("#canvas")[0];
var gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');

function fixCanvas() {
    var rect = canvas.parentNode.getBoundingClientRect();
    canvas.width = rect.width;
    canvas.height = rect.height;
    gl.uniform2f(resolution_location, canvas.width, canvas.height);
    var mx = Math.max(canvas.width, canvas.height);
    gl.uniform2f(screen_ratio_location, canvas.width / mx, canvas.height / mx);
    gl.viewport(0, 0, gl.canvas.width, gl.canvas.height);
}

//SHADER EDITOR ============================================
$("#expand-button").click(function () {
    $("#editor").toggleClass("expanded");
});
$(".list-button").click(function () {
    $('.list-button').removeClass('active');
    $(this).addClass('active');
    $(this).data('code');
});
$("#run").click(function () {
    run();
});
var screen_ratio_location;
var resolution_location;
var playerPos_location;
var playerFwd_location;
var playerUp_location;
var playerRight_location;
var time_location;
var detail_location;
var shadow_location;
var render_location;
var vShader = gl.createShader(gl.VERTEX_SHADER);
gl.shaderSource(vShader, vert);
gl.compileShader(vShader);
function run() {
    var fractal = codify($('#fractal'));
    var d = $('#draw-region').children().eq(0).val() + ',' + $('#draw-region').children().eq(1).val() + ',' + $('#draw-region').children().eq(2).val();
    d = toFloats(d.split(','));
    fractal += `return box(z.xyz,vec3(0),vec3(${d[0]},${d[1]},${d[2]}))/abs(z.w);\n`;
    //console.log(fractal);
    var color = codify($('#color'));
    console.log(color);
    var Nfrag = frag.replace('INSERTFRACTALHERE', fractal);
    Nfrag = Nfrag.replace('INSERTCOLORHERE', color);
    //console.log(Nfrag);
    //var program = gl.createProgram();
    var fShader = gl.createShader(gl.FRAGMENT_SHADER);
    gl.shaderSource(fShader, Nfrag);
    gl.compileShader(fShader);
    var compilationLog = gl.getShaderInfoLog(fShader);
    console.log('Shader compiler log: ' + compilationLog);
    var program = gl.createProgram();
    gl.attachShader(program, vShader);
    gl.attachShader(program, fShader);
    gl.linkProgram(program);
    gl.deleteShader(vShader);
    gl.deleteShader(fShader);
    gl.useProgram(program);
    screen_ratio_location = gl.getUniformLocation(program, "screen_ratio");
    var position_location = gl.getAttribLocation(program, "position");
    resolution_location = gl.getUniformLocation(program, "resolution");
    playerPos_location = gl.getUniformLocation(program, "playerPos");
    playerFwd_location = gl.getUniformLocation(program, "playerFwd");
    playerUp_location = gl.getUniformLocation(program, "playerUp");
    playerRight_location = gl.getUniformLocation(program, "playerRight");
    time_location = gl.getUniformLocation(program, "time");
    shadow_location = gl.getUniformLocation(program, "shadow_count");
    detail_location = gl.getUniformLocation(program, "detail");
    render_location = gl.getUniformLocation(program, "render_count");
    gl.uniform1f(detail_location, Math.pow(10, -$('#detail').val()));
    gl.uniform1i(shadow_location, $('#shadow').val());
    gl.uniform1i(render_location, $('#render').val());
    var buffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1.0, -1.0, 1.0, -1.0, -1.0, 1.0, -1.0, 1.0, 1.0, -1.0, 1.0, 1.0]), gl.STATIC_DRAW);
    gl.enableVertexAttribArray(position_location);
    gl.vertexAttribPointer(position_location, 2, gl.FLOAT, false, 0, 0);
    fixCanvas();
}

function save() {
    var encodedFractal = encodeList($('#fractal'));
    var encodedColor = encodeList($('#color'));
    var d = $('#draw-region').children().eq(0).val() + ',' + $('#draw-region').children().eq(1).val() + ',' + $('#draw-region').children().eq(2).val();
    window.history.replaceState(null, null, "?fractal=" + encodedFractal + "&draw=" + d + "&color=" + encodedColor);
}

function makeTextChangesSave() {
    $('input').bind('input', function () {
        save();
    });
}
makeTextChangesSave();

function encodeList(node) {
    var encoded = '';
    node.children().each(function () {
        encoded += encodeNode($(this));
    });
    return encoded;
}

function encodeVector(node) {
    return node.children().eq(0).val() + "," + node.children().eq(1).val() + "," + node.children().eq(2).val();
}

function decodeVector(node, vector) {
    node.children().eq(0).val(vector[0]);
    node.children().eq(1).val(vector[1]);
    node.children().eq(2).val(vector[2]);
}
function encodeNode(node) {
    var encoded;
    var children = node.children();
    switch (node.data('name')) {
        case 'Rotate':
            encoded = 'FR' + encodeVector(children.eq(1));
            break;
        case 'Repeat':
            encoded = 'FC' + children.eq(0).children().eq(1).val();
            encoded += encodeList(children.eq(1)) + 'FE';
            break;
        case 'Translate':
            encoded = 'FT' + encodeVector(children.eq(1));
            break;
        // case 'Box':
        //     encoded = 'FB' + encodeVector(children.eq(1));
        //     encoded += ',' + encodeVector(children.eq(2));
        //     break;
        // case 'Sphere':
        //     encoded = 'FS' + encodeVector(children.eq(1)) + ',' + children.eq(2).val();
        //     break;
        case 'Box_Fold':
            encoded = 'FH' + children.eq(1).val();
            break;
        case 'Sphere_Fold':
            encoded = 'FO' + children.eq(1).val() + ',' + children.eq(2).val();
            break;
        case 'Mandel':
            encoded = 'FM' + children.eq(1).val();
            break;
        case 'Menger':
            encoded = 'FA' + encodeVector(children.eq(1)) + ',' + children.eq(2).val();
            break;
        case 'Tetrahedral':
            encoded = 'FW';
            break;
        case 'Scale':
            encoded = 'FQ' + children.eq(1).val();
            break;
        case 'Orbit_Trap':
            encoded = 'FB' + children.eq(0).children().eq(1).val() + ',' + children.eq(0).children().eq(2).val();
            encoded += encodeList(children.eq(1)) + 'FX' + children.eq(0).children().eq(2).val();
            break;
    }
    return encoded;
}
function decodeSave(saved, parentNode) {
    for (var f of saved.split('F')) {
        var newNode = null;
        var nextParentNode = parentNode;
        switch (f.charAt(0)) {
            case 'R':
                newNode = $(".master[data-name='Rotate']").clone().removeClass("master");
                decodeVector(newNode.children().eq(1), f.substring(1).split(','));
                break;
            case 'C':
                newNode = $(".master[data-name='Repeat']").clone().removeClass("master");
                newNode.children().eq(0).children().eq(1).val(f.substring(1));
                nextParentNode = newNode.find(".codeblock-list-master").removeClass("codeblock-list-master").addClass("codeblock-list");
                break;
            case 'E':
                nextParentNode = parentNode.parent().closest('.codeblock-list');
                break;
            case 'T':
                newNode = $(".master[data-name='Translate']").clone().removeClass("master");
                decodeVector(newNode.children().eq(1), f.substring(1).split(','));
                break;
            // case 'B':
            //     newNode = $(".master[data-name='Box']").clone().removeClass("master");
            //     decodeVector(newNode.children().eq(1), f.substring(1).split(','));
            //     decodeVector(newNode.children().eq(2), f.substring(1).split(',').slice(3));
            //     break;
            // case 'S':
            //     newNode = $(".master[data-name='Sphere']").clone().removeClass("master");
            //     decodeVector(newNode.children().eq(1), f.substring(1).split(','));
            //     newNode.children().eq(2).val(f.substring(1).split(',')[3]);
            //     break;
            case 'H':
                newNode = $(".master[data-name='Box_Fold']").clone().removeClass("master");
                newNode.children().eq(1).val(f.substring(1));
                break;
            case 'O':
                newNode = $(".master[data-name='Sphere_Fold']").clone().removeClass("master");
                newNode.children().eq(1).val(f.substring(1).split(',')[0]);
                newNode.children().eq(2).val(f.substring(1).split(',')[1]);
                break;
            case 'M':
                newNode = $(".master[data-name='Mandel']").clone().removeClass("master");
                newNode.children().eq(1).val(f.substring(1));
                break;
            case 'A':
                newNode = $(".master[data-name='Menger']").clone().removeClass("master");
                decodeVector(newNode.children().eq(1), f.substring(1).split(','));
                newNode.children().eq(2).val(f.substring(1).split(',')[3]);
                break;
            case 'W':
                newNode = $(".master[data-name='Tetrahedral']").clone().removeClass("master");
                break;
            case 'Q':
                newNode = $(".master[data-name='Scale']").clone().removeClass("master");
                newNode.children().eq(1).val(f.substring(1));
                break;
            case 'B':
                newNode = $(".master[data-name='Orbit_Trap']").clone().removeClass("master");
                newNode.children().eq(0).children().eq(1).val(f.substring(1).split(',')[0]);
                newNode.children().eq(0).children().eq(2).val(f.substring(1).split(',')[1]);
                nextParentNode = newNode.find(".codeblock-list-master").removeClass("codeblock-list-master").addClass("codeblock-list");
                break;
            case 'X':
                nextParentNode = parentNode.parent().closest('.codeblock-list');
                break;
        }
        if (newNode != null) {
            parentNode.append(newNode);
        }
        parentNode = nextParentNode;
    }
    makeTextChangesSave();
}
function toFloats(p) {
    var f = [];
    for (var n = 0; n < p.length; n++) {
        if (!p[n].includes('.')) {
            f[n] = p[n] + '.0';
        } else {
            f[n] = p[n];
        }
    }
    return f;
}
function toInts(p) {
    var i = [];
    for (var n = 0; n < p.length; n++) {
        i[n] = p[n] | 0;
    }
    return i;
}
function codify(node) {
    var code = '';
    var encoded = encodeList(node);
    for (var f_ of encoded.split('F')) {
        var p = f_.substring(1).split(',');
        var i = toInts(p);
        var f = toFloats(p);
        console.log(i);
        console.log(f);

        switch (f_.charAt(0)) {
            case 'R':
                code += `rotate(z,vec3(${f[0] * 0.0174533},${f[1] * 0.0174533},${f[2] * 0.0174533}));\n`;
                break;
            case 'C':
                code += `for(int i = 0; i < ${i[0]}; i++){\n`;
                break;
            case 'E':
                code += `}\n`;
                break;
            case 'T':
                code += `translate(z,vec3(${f[0]},${f[1]},${f[2]}));\n`;
                break;
            // case 'B':
            //     code += `return box(z.xyz,vec3(${f[0]},${f[1]},${f[2]}),vec3(${f[3]},${f[4]},${f[5]}))/abs(z.w);\n`;
            //     break;
            // case 'S':
            //     code += `return sphere(z.xyz,vec3(${f[0]},${f[1]},${f[2]}),${f[3]})/abs(z.w);\n`;
            //     break;
            case 'H':
                code += `box_fold(z,${f[0]});\n`;
                break;
            case 'O':
                code += `sphere_fold(z,${f[0]},${f[1]});\n`;
                break;
            case 'M':
                code += `mandel(z,pos,${f[0]});\n`;
                break;
            case 'A':
                code += `menger(z,vec3(${f[0]},${f[1]},${f[2]}),${f[3]});\n`;
                break;
            case 'W':
                code += `tetrahedral(z);\n`;
                break;
            case 'Q':
                code += `scale(z,${f[0]});\n`;
                break;
            case 'B':
                code += `for(int i = 0; i < ${i[0]}; i++){\n`;
                break;
            case 'X':
                code += `orbit = min(orbit, abs(length(z.xyz) / z.w));}\n`;
                code += `return hsv2rgb(vec3(orbit * ${f[0]}, 0.5, 0.5));`;
                break;
        }
    }
    return code;
}

//Dragging ====================
var dragging = null;
var dragStart = {};
var target;
var lastMouse = {};
var releasedIn;
var rawReleasedIn;
function findObject(obj, searchClass, endClasses) {
    var nUp = obj;
    while (!nUp.hasClass(searchClass)) {
        for (const _class of endClasses) {
            if (nUp.hasClass(_class)) return null;
        }
        nUp = nUp.parent();
        if (nUp.is("body") || nUp.length == 0) return null;
    }
    return nUp;
}

function dragRelease() {
    if (dragging == null) return;
    dragging.css("z-index", 12);
    var copy;
    if (lastMouse.awaitingMovement) {
        $(".selected").removeClass("selected");
        if (!dragging.hasClass("master")) {
            dragging.addClass("selected");
        }
    } else if (releasedIn != null) {
        $(".indicator").after(dragging);
        dragging.css("position", "unset");
        dragging.css("z-index", "unset");

    } else {
        if (rawReleasedIn.attr("id") == 'trash') {
            dragging.remove();
        }
        if (rawReleasedIn.attr("id") == 'copy') {
            var copy = dragging.clone().css('top', parseInt(dragging.css('top')) - 10);
            copy.css('left', parseInt(copy.css('left')) + 10);
        }
    }
    $(".codeblock-list").removeClass("glow");
    $(".indicator").remove();
    dragging.removeClass("invisible");
    $("body")[0].onmousemove = null;
    //$(".codeblock:not(.master)").css("z-index", 11);
    if (copy != null) {
        copy.appendTo(dragging.parent());
        copy.removeClass("invisible");
        copy.css("z-index", 13);
    }
    dragging = null;
    makeTextChangesSave();
    save();
}

function placeIndicator(e, force = false) {
    if (lastMouse.x != e.clientX || lastMouse.y != e.clientY) {
        rawReleasedIn = target;
        releasedIn = findObject(rawReleasedIn, "codeblock-list", ["main"]);
        if (lastMouse.awaitingMovement) {
            lastMouse.awaitingMovement = false;
            dragging.css("position", "absolute");
            dragging.css("top", dragStart.blockOffset.top);
            dragging.css("left", dragStart.blockOffset.left);
            dragging.css("z-index", 999);
            dragging.appendTo($("body"));
        }
        if (releasedIn != null) {
            $(".codeblock-list").removeClass("glow");
            releasedIn.addClass("glow");
            var placed = false;
            $(".indicator").remove();
            releasedIn.children().each(function () {
                if ($(this)[0] != dragging[0] && $(this).offset().top + $(this).height() / 2 > e.clientY) {
                    placed = true;
                    $("<div class='indicator'></div>").insertBefore($(this));
                    return false;
                }
            });
            if (!placed) {
                $("<div class='indicator'></div>").appendTo(releasedIn);
            }
            $(".indicator").width(dragging.width());
            $(".indicator").height(dragging.height() + 2);
        } else {
            $(".indicator").remove();
            $(".codeblock-list").removeClass("glow");
        }
    }
}

$("body").mousedown((e) => {
    releasedIn = null;
    dragging = findObject($(e.target), "codeblock", ["codeblock-numfield", "main", "codeblock-vector"]);
    if (dragging == null) {
        $(".selected").removeClass("selected");
        return;
    }
    dragStart.blockOffset = dragging.offset();
    dragStart.mouseX = e.clientX;
    dragStart.mouseY = e.clientY;
    lastMouse.awaitingMovement = true;
    lastMouse.x = e.clientX;
    lastMouse.y = e.clientY;
    if (dragging.hasClass("master")) {
        dragging = dragging.clone().removeClass("master");
        dragging.find(".codeblock-list-master").removeClass("codeblock-list-master").addClass("codeblock-list");
        makeTextChangesSave();
    }
    dragging.addClass("invisible");

    $("body").mousemove(function (e) {
        if (dragging == null) return;
        target = $(e.target);
        placeIndicator(e);
        lastMouse.x = e.clientX;
        lastMouse.y = e.clientY;
        dragging.css("top", e.clientY - dragStart.mouseY + dragStart.blockOffset.top);
        dragging.css("left", e.clientX - dragStart.mouseX + dragStart.blockOffset.left);
    }).mouseup(dragRelease);
});
$(document).mouseleave(dragRelease);
$(document).keyup(function (e) {
    if (e.keyCode == 46) {
        $(".selected").remove();
    }
});

//LOAD SHADER=====================================
var params = {};
location.search.replace(/[?&]+([^=&]+)=([^&]*)/gi, function (s, k, v) { params[k] = v })
if (params.fractal == undefined) {
    //$('.list-button.active').data('code');
} else {
    decodeSave(params.fractal, $('#fractal'));
    decodeSave(params.color, $('#color'));
    var d = params.draw.split(',');
    $('#draw-region').children().eq(0).val(d[0]);
    $('#draw-region').children().eq(1).val(d[1]);
    $('#draw-region').children().eq(2).val(d[2]);
    run();
}

//CREATE SHADER=============================
// var vShader = gl.createShader(gl.VERTEX_SHADER);
// var fShader = gl.createShader(gl.FRAGMENT_SHADER);
// gl.shaderSource(vShader, vert);
// gl.shaderSource(fShader, frag);
// gl.compileShader(vShader);
// gl.compileShader(fShader);
// var compilationLog = gl.getShaderInfoLog(fShader);
// console.log('Shader compiler log: ' + compilationLog);
// var program = gl.createProgram();
// gl.attachShader(program, vShader);
// gl.attachShader(program, fShader);
// gl.linkProgram(program);
// //gl.deleteShader(vShader);
// gl.deleteShader(fShader);
// gl.useProgram(program);


//CONTROLS ===========================================
canvas.onmousedown = function (event) {
    var x = event.clientX;
    var y = event.clientY;
    var rect = event.target.getBoundingClientRect();
    if (rect.left <= x && rect.right > x &&
        rect.top <= y && rect.bottom > y) {
        lastX = x;
        lastY = y;
        mouseDown = true;
    }
}
canvas.onmouseup = function (event) {
    mouseDown = false;
}
function degToRad(d) {
    return d * Math.PI / 180;
}
canvas.onmousemove = function (event) {
    var x = event.clientX;
    var y = event.clientY;
    if (mouseDown) {
        yawAngle += (x - lastX) / 4;
        pitchAngle += (y - lastY) / 4;
        pitchAngle = Math.max(Math.min(pitchAngle, 89.999), -89.999);
    }
    lastX = x;
    lastY = y;
}

var speed_range = document.getElementById("speed");
document.onmousewheel = function (event) {
    speed += Math.sign(event.wheelDelta);
    speed = Math.max(Math.min(speed, 20), -40);
    speed_range.value = Math.round(speed);
}
speed_range.addEventListener('input', function () {
    speed = parseInt(speed_range.value);
});
document.getElementById("detail").addEventListener('input', function () {
    gl.uniform1f(detail_location, Math.pow(10, -parseFloat(document.getElementById("detail").value)));
});
document.getElementById("shadow").addEventListener('input', function () {
    gl.uniform1i(shadow_location, parseInt(document.getElementById("shadow").value));
});
document.getElementById("render").addEventListener('input', function () {
    gl.uniform1i(render_location, parseInt(document.getElementById("render").value));
});
window.addEventListener("keydown", onKeyDown, false);
window.addEventListener("keyup", onKeyUp, false);
function onKeyDown(event) {
    var keyCode = event.keyCode;
    switch (keyCode) {
        case 68: //d
            move[0] = 1;
            break;
        case 83: //s
            move[2] = -1;
            break;
        case 65: //a
            move[0] = -1;
            break;
        case 87: //w
            move[2] = 1;
            break;
    }
}
function onKeyUp(event) {
    var keyCode = event.keyCode;

    switch (keyCode) {
        case 68: //d
            move[0] = 0;
            break;
        case 83: //s
            move[2] = 0;
            break;
        case 65: //a
            move[0] = 0;
            break;
        case 87: //w
            move[2] = 0;
            break;
    }
}

function movement(deltaTime) {
    var fwd = vec3.fromValues(0, 0, 1);
    vec3.rotateX(fwd, fwd, [0, 0, 0], degToRad(-pitchAngle));
    vec3.rotateY(fwd, fwd, [0, 0, 0], degToRad(-yawAngle));
    var right = vec3.fromValues(0, 0, 0);
    var up = vec3.fromValues(0, 1, 0)
    vec3.cross(right, up, fwd);
    vec3.normalize(right, right);
    vec3.cross(up, right, fwd);
    vec3.normalize(up, up);

    var rightMove = vec3.create();
    var rSpeed = Math.pow(10, speed / 10);
    vec3.scale(rightMove, right, move[0] * deltaTime * rSpeed);
    var fwdMove = vec3.create();
    vec3.scale(fwdMove, fwd, move[2] * deltaTime * rSpeed);
    vec3.add(playerPos, playerPos, rightMove);
    vec3.add(playerPos, playerPos, fwdMove);

    gl.uniform3f(playerUp_location, up[0], up[1], up[2]);
    gl.uniform3f(playerRight_location, right[0], right[1], right[2]);
    gl.uniform3f(playerFwd_location, fwd[0], fwd[1], fwd[2]);
    gl.uniform3f(playerPos_location, playerPos[0], playerPos[1], playerPos[2]);
}

var start = Date.now();
var lastFrame = start;
function render() {
    var current = Date.now();
    var elapsed = current - lastFrame;
    lastFrame = current;
    gl.uniform1f(time_location, current - start);
    movement(elapsed / 1000);
    document.getElementById("fps").textContent = Math.round(1000 / elapsed);
    gl.drawArrays(gl.TRIANGLES, 0, 6);

    requestAnimationFrame(render, canvas);
}
render();
